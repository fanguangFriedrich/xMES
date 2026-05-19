using Infrastructure;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAuth.App.DingTalk.Request;
using OpenAuth.App.DingTalk.Response;
using OpenAuth.App.Request;
using OpenAuth.Repository.Domain;
using OpenAuth.Repository.Domain.DingTalk;
using SqlSugar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAuth.App.DingTalk
{
    public class DingTalkApp
    {
        private readonly HttpClient _httpClient;
        private readonly DingTalkOptions _options;
        private readonly ILogger<DingTalkApp> _logger;
        private readonly ISqlSugarClient _sqlSugarClient;
        private readonly OrgManagerApp _orgManagerApp;
        private readonly UserManagerApp _userManagerApp;

        private string? _cachedCorpAccessToken;
        private DateTime _corpAccessTokenExpiry = DateTime.MinValue;

        // 并发控制：同时最多3个请求，避免触发钉钉限流
        private readonly SemaphoreSlim _throttle = new(3, 3);
        private const int RetryCount = 3;
        private const int RetryBaseMs = 1500;

        // 用户授权码模式
        private const string TokenApiUrl = "https://api.dingtalk.com/v1.0/oauth2/userAccessToken";
        private const string UserInfoApiUrl = "https://api.dingtalk.com/v1.0/contact/users/me";

        // 企业内部应用模式（对应Java的 client_credentials）
        private const string CorpTokenApiUrl = "https://api.dingtalk.com/v1.0/oauth2/accessToken";
        private const string CorpUserInfoApiUrl = "https://oapi.dingtalk.com/topapi/v2/user/getuserinfo";
        private const string UserGetApiUrl = "https://oapi.dingtalk.com/topapi/v2/user/get";
        private const string UserGetByUnionIdApiUrl = "https://oapi.dingtalk.com/topapi/user/getbyunionid";

        private const string UserListApiUrl = "https://oapi.dingtalk.com/topapi/v2/user/list";
        private const string DeptListSubApiUrl = "https://oapi.dingtalk.com/topapi/v2/department/listsub";
        private const string DeptGetApiUrl = "https://oapi.dingtalk.com/topapi/v2/department/get";
        private const string WorkNotificationApiUrl = "https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2";

        public DingTalkApp(
        HttpClient httpClient,
        IOptions<DingTalkOptions> options,
        ILogger<DingTalkApp> logger,
        ISqlSugarClient sqlSugarClient,
        OrgManagerApp orgManagerApp,
        UserManagerApp userManagerApp)
        {
            _httpClient      = httpClient;
            _options         = options.Value;
            _logger          = logger;
            _sqlSugarClient  = sqlSugarClient;
            _orgManagerApp   = orgManagerApp;
            _userManagerApp  = userManagerApp;
        }

        private async Task<string> GetValidCorpAccessTokenAsync()
        {
            // 提前5分钟刷新，避免临界点过期
            if (_cachedCorpAccessToken != null && DateTime.UtcNow < _corpAccessTokenExpiry.AddMinutes(-5))
            {
                return _cachedCorpAccessToken;
            }

            var corpId = _options.CorpId;
            (_cachedCorpAccessToken, _corpAccessTokenExpiry) = await GetCorpAccessTokenAsync(corpId);

            Console.WriteLine($"[DingTalk] AccessToken 已刷新，有效期至: {_corpAccessTokenExpiry:yyyy-MM-dd HH:mm:ss} UTC");

            return _cachedCorpAccessToken;
        }

        /// <summary>
        /// 获取钉钉配置信息（供前端使用，不返回 ClientSecret）
        /// </summary>
        public DingTalkOptions GetDingTalkOptions()
        {
            return new DingTalkOptions
            {
                ClientId = _options.ClientId,
                CorpId = _options.CorpId,
                // ClientSecret 不对外暴露
                ClientSecret = string.Empty
            };
        }

        /// <summary>
        /// 通过授权码获取用户信息
        /// </summary>
        public async Task<DingTalkUserInfo> GetUserByAuthCodeAsync(string authCode)
        {
            var accessToken = await GetAccessTokenAsync(authCode);
            return await GetUserInfoAsync(accessToken);
        }

        /// <summary>
        /// 通过授权码获取用户详细信息
        /// <param name="authCode">钉钉授权码</param>
        /// </summary>
        public async Task<DingTalkUserDetailInfo> GetUserDetailInfoByAuthCodeAsync(string authCode)
        {
            var userInfo = await GetUserByAuthCodeAsync(authCode);
            string userId= await GetUserIdByUnionIdAsync(userInfo.UnionId);
            var userDetailInfo = await GetUserDetailInfoByUserIdAsync(userId);

            if(userDetailInfo == null)
            {
                throw new Exception("通过授权码获取用户详细信息失败");
            }

            return userDetailInfo;
        }

        /// <summary>
        /// 通过授权码获取 AccessToken
        /// </summary>
        public async Task<string> GetAccessTokenAsync(string authCode)
        {
            var body = new
            {
                clientId = _options.ClientId,
                clientSecret = _options.ClientSecret,
                code = authCode,
                grantType = "authorization_code"
            };

            var content = new StringContent(
                JsonHelper.SerializeCamelCase(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(TokenApiUrl, content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonHelper.Deserialize<DingTalkTokenResponse>(resultJson);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                throw new Exception("获取钉钉 AccessToken 失败");

            return tokenResponse.AccessToken;
        }

        /// <summary>
        /// 通过 AccessToken 获取用户信息
        /// </summary>
        public async Task<DingTalkUserInfo> GetUserInfoAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, UserInfoApiUrl);
            request.Headers.Add("x-acs-dingtalk-access-token", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            var userInfo = JsonHelper.Deserialize<DingTalkUserInfo>(resultJson);

            if (userInfo == null)
                throw new Exception("获取钉钉用户信息失败");

            return userInfo;
        }

        public async Task<DingTalkWorkNotificationResponse> SendWorkNotificationAsync(DingTalkWorkNotificationReq req)
        {
            ValidateWorkNotificationReq(req);

            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{WorkNotificationApiUrl}?access_token={Uri.EscapeDataString(corpAccessToken)}";

            var bodyObj = new Dictionary<string, object>
            {
                ["agent_id"] = ResolveAgentId(req.AgentId),
                ["msg"] = req.Msg
            };

            if (!string.IsNullOrWhiteSpace(req.UseridList))
                bodyObj["userid_list"] = req.UseridList;

            if (!string.IsNullOrWhiteSpace(req.DeptIdList))
                bodyObj["dept_id_list"] = req.DeptIdList;

            if (req.ToAllUser.HasValue)
                bodyObj["to_all_user"] = req.ToAllUser.Value;

            var content = new StringContent(
                JsonHelper.Serialize(bodyObj),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] SendWorkNotification 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"发送工作通知失败 [{response.StatusCode}]: {resultJson}");

            var result = JsonHelper.Deserialize<DingTalkWorkNotificationResponse>(resultJson)
                ?? throw new Exception("解析发送工作通知响应失败");

            if (result.Errcode != 0)
                throw new Exception($"发送工作通知失败 errcode={result.Errcode}: {result.Errmsg}");

            return result;
        }

        public Task<DingTalkWorkNotificationResponse> SendTextWorkNotificationAsync(DingTalkTextWorkNotificationReq req)
        {
            if (req == null)
                throw new ArgumentNullException(nameof(req));

            if (string.IsNullOrWhiteSpace(req.Content))
                throw new ArgumentException("消息内容不能为空", nameof(req.Content));

            var msgJson = JsonHelper.Serialize(new
            {
                msgtype = "text",
                text = new
                {
                    content = req.Content
                }
            });

            using var doc = JsonDocument.Parse(msgJson);
            var sendReq = new DingTalkWorkNotificationReq
            {
                UseridList = req.UseridList,
                DeptIdList = req.DeptIdList,
                ToAllUser = req.ToAllUser,
                AgentId = req.AgentId,
                Msg = doc.RootElement.Clone()
            };

            return SendWorkNotificationAsync(sendReq);
        }

        private long ResolveAgentId(long? agentId)
        {
            if (agentId.HasValue && agentId.Value > 0)
                return agentId.Value;

            if (long.TryParse(_options.AgentId, out var configuredAgentId) && configuredAgentId > 0)
                return configuredAgentId;

            throw new Exception("钉钉 AgentId 未配置或格式不正确");
        }

        private static void ValidateWorkNotificationReq(DingTalkWorkNotificationReq req)
        {
            if (req == null)
                throw new ArgumentNullException(nameof(req));

            if (req.Msg.ValueKind == JsonValueKind.Undefined || req.Msg.ValueKind == JsonValueKind.Null)
                throw new ArgumentException("消息体msg不能为空", nameof(req.Msg));

            var hasUserIds = !string.IsNullOrWhiteSpace(req.UseridList);
            var hasDeptIds = !string.IsNullOrWhiteSpace(req.DeptIdList);
            var toAllUser = req.ToAllUser == true;
            if (!hasUserIds && !hasDeptIds && !toAllUser)
                throw new ArgumentException("userid_list、dept_id_list、to_all_user 必须至少提供一个");
        }

        // ─────────────────────────────────────────────
        // 企业内部应用模式（对应 Java Controller 逻辑）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 通过免登授权码 获取用户信息（企业内部应用模式）
        /// 对应 Java: GET /api/getUserInfo?code=xxx&corpId=xxx
        /// </summary>
        public async Task<DingTalkUserDetailInfo> GetUserDetailInfoByCorpCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code 不能为空", nameof(code));

            var corpAccessToken = await GetValidCorpAccessTokenAsync();

            var url = $"{CorpUserInfoApiUrl}?access_token={Uri.EscapeDataString(corpAccessToken)}";
            var content = new StringContent(
                JsonHelper.SerializeCamelCase(new { code }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetCorpUserInfo 响应: {resultJson}");

            var result = JsonHelper.Deserialize<DingTalkCorpUserInfoResponse>(resultJson);

            if (result == null || result.Errcode != 0)
                throw new Exception($"获取企业用户信息失败: {result?.Errmsg}");

            var userId = result.Result?.UserId
                ?? throw new Exception("获取用户信息失败：userId 为空");

            return await GetUserDetailInfoByUserIdAsync(userId);
        }

        /// <summary>
        /// 通过 client_credentials 模式获取企业 AccessToken
        /// 对应 Java: getAccessToken(clientId, clientSecret, corpId)
        /// </summary>
        public async Task<(string Token, DateTime Expiry)> GetCorpAccessTokenAsync(string corpId)
        {
            var body = new
            {
                appKey = _options.ClientId,
                appSecret = _options.ClientSecret,
                corpId = corpId,
                grantType = "client_credentials"
            };

            var content = new StringContent(
                JsonHelper.SerializeCamelCase(body),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Remove("x-acs-dingtalk-access-token");
            var response = await _httpClient.PostAsync(CorpTokenApiUrl, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] 响应体: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取企业 AccessToken 失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("accessToken", out var tokenProp))
                throw new Exception($"响应中无 accessToken 字段: {resultJson}");

            if (!root.TryGetProperty("expireIn", out var expireInProp))
                throw new Exception($"响应中无 expireIn 字段: {resultJson}");

            var token = tokenProp.GetString() ?? throw new Exception("accessToken 为空");
            var expiry = DateTime.UtcNow.AddSeconds(expireInProp.GetInt32());

            return (token, expiry);
        }


        public async Task<List<long>> GetSubDeptIdListAsync(long deptId)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"https://oapi.dingtalk.com/topapi/v2/department/listsubid?access_token={corpAccessToken}";

            var content = new StringContent(
                JsonHelper.SerializeCamelCase(new { dept_id = deptId }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetSubDeptIdList 响应体: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取子部门ID列表失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("errcode", out var errcodeProp) || errcodeProp.GetInt32() != 0)
            {
                var errmsg = root.TryGetProperty("errmsg", out var errmsgProp) ? errmsgProp.GetString() : "未知错误";
                throw new Exception($"获取子部门ID列表失败: {errmsg}，响应: {resultJson}");
            }

            if (!root.TryGetProperty("result", out var resultProp))
                throw new Exception($"响应中无 result 字段: {resultJson}");

            if (!resultProp.TryGetProperty("dept_id_list", out var deptIdListProp))
                throw new Exception($"result 中无 dept_id_list 字段: {resultJson}");

            return deptIdListProp.EnumerateArray()
                .Select(item => item.GetInt64())
                .ToList();
        }

        public async Task TraverseDeptAsync(long deptId, List<long> allDeptIds)
        {
            var subDeptIds = await GetSubDeptIdListAsync(deptId);
            if (subDeptIds == null || subDeptIds.Count == 0)
                return;

            foreach (var subDeptId in subDeptIds)
            {
                allDeptIds.Add(subDeptId);
                // 递归获取子部门的子部门
                await TraverseDeptAsync(subDeptId, allDeptIds);
            }
        }

        /// <summary>
        /// 获取指定部门下所有员工信息（自动翻页）
        /// </summary>
        public async Task<List<DingTalkUserDetailInfo>> GetDeptUserListAsync(long deptId)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{UserListApiUrl}?access_token={corpAccessToken}";
            var allUsers = new List<DingTalkUserDetailInfo>();
            long cursor = 0;
            const int size = 50;
            bool hasMore = true;

            while (hasMore)
            {
                var body = new
                {
                    dept_id = deptId,
                    cursor = cursor,
                    size = size,
                    contain_access_limit = false,
                    language = "zh_CN"
                };

                var content = new StringContent(JsonHelper.Serialize(body), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var resultJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[DingTalk] GetDeptUserList deptId={deptId} cursor={cursor} 响应: {resultJson}");

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"获取部门员工列表失败 [{response.StatusCode}]: {resultJson}");

                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;

                var errcode = root.GetProperty("errcode").GetInt32();
                if (errcode != 0)
                {
                    var errmsg = root.GetProperty("errmsg").GetString();
                    throw new Exception($"获取部门员工列表失败: {errmsg}");
                }

                var result = root.GetProperty("result");

                hasMore = result.GetProperty("has_more").GetBoolean();

                if (hasMore && result.TryGetProperty("next_cursor", out var nextCursorProp))
                    cursor = nextCursorProp.GetInt64();
                else
                    hasMore = false;

                if (result.TryGetProperty("list", out var list))
                {
                    var items = list.ValueKind switch
                    {
                        JsonValueKind.Array => list.EnumerateArray(),
                        JsonValueKind.Object => new[] { list }.Select(x => x),  // 统一成 IEnumerable
                        _ => Enumerable.Empty<JsonElement>()
                    };

                    foreach (var item in items)
                    {
                        var user = JsonHelper.Deserialize<DingTalkUserDetailInfo>(item.GetRawText());
                        if (user is not null)
                            allUsers.Add(user);
                    }
                }

                if (hasMore)
                    await Task.Delay(100);
            }

            return allUsers;
        }

        /// <summary>
        /// 获取企业所有部门下的所有员工（去重）
        /// </summary>
        public async Task<List<DingTalkUserDetailInfo>> GetAllUsersAsync()
        {
            // 先获取所有部门ID
            var allDeptIds = new List<long>();
            await TraverseDeptAsync(1, allDeptIds);

            var userDict = new Dictionary<string, DingTalkUserDetailInfo>();

            foreach (var deptId in allDeptIds)
            {
                var users = await GetDeptUserListAsync(deptId);
                foreach (var user in users)
                {
                    // 以 userId 去重，同一员工可能属于多个部门
                    if (user.UserId != null && !userDict.ContainsKey(user.UserId))
                    {
                        userDict[user.UserId] = user;
                    }
                }
                await Task.Delay(100);
            }

            return userDict.Values.ToList();
        }

        /// <summary>
        /// 通过 userId 获取用户详细信息
        /// </summary>
        public async Task<DingTalkUserDetailInfo> GetUserDetailInfoByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId 不能为空", nameof(userId));

            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{UserGetApiUrl}?access_token={corpAccessToken}";

            var content = new StringContent(
                JsonHelper.Serialize(new { userid = userId, language = "zh_CN" }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetUserByUserId userId={userId} 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取用户详情失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            var errcode = root.GetProperty("errcode").GetInt32();
            if (errcode != 0)
                throw new Exception($"获取用户详情失败: {root.GetProperty("errmsg").GetString()}");

            return JsonHelper.Deserialize<DingTalkUserDetailInfo>(root.GetProperty("result").GetRawText())
                ?? throw new Exception("解析用户详情失败");
        }

        /// <summary>
        /// 通过 unionId 获取用户 userId
        /// </summary>
        public async Task<string> GetUserIdByUnionIdAsync(string unionId)
        {
            if (string.IsNullOrWhiteSpace(unionId))
                throw new ArgumentException("unionId 不能为空", nameof(unionId));

            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{UserGetByUnionIdApiUrl}?access_token={corpAccessToken}";

            var content = new StringContent(
                JsonHelper.Serialize(new { unionid = unionId }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetUserIdByUnionId unionId={unionId} 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"通过unionId获取userId失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            var errcode = root.GetProperty("errcode").GetInt32();
            if (errcode != 0)
                throw new Exception($"通过unionId获取userId失败: {root.GetProperty("errmsg").GetString()}");

            return root.GetProperty("result").GetProperty("userid").GetString()
                ?? throw new Exception("userid 为空");
        }

        /// <summary>
        /// 递归获取企业所有部门列表（含详情）
        /// </summary>
        public async Task<List<DingTalkDeptInfo>> GetAllDeptListAsync(long rootDeptId=1)
        {
            var allDepts = new ConcurrentBag<DingTalkDeptInfo>();
            // 全局信号量，控制整棵树的并发请求数
            var semaphore = new SemaphoreSlim(5);

            await TraverseDeptListAsync(rootDeptId, allDepts, semaphore);

            return allDepts.ToList();
        }

        public async Task<List<DingTalkDeptInfo>> GetAllDeptListWithRootAsync(long rootDeptId = 1)
        {
            var allDepts = await GetAllDeptListAsync(rootDeptId);

            if (!allDepts.Any(d => d.DeptId == rootDeptId))
            {
                var rootDept = await GetDeptByIdAsync(rootDeptId);
                allDepts.Insert(0, rootDept);
            }

            return allDepts
                .GroupBy(d => d.DeptId)
                .Select(g => g.First())
                .ToList();
        }

        private async Task TraverseDeptListAsync(
    long deptId,
    ConcurrentBag<DingTalkDeptInfo> allDepts,
    SemaphoreSlim semaphore)
        {
            List<DingTalkDeptInfo> subDepts;

            await semaphore.WaitAsync();
            try
            {
                await Task.Delay(50); // 全局限流保护，可根据钉钉QPS调整
                subDepts = await GetSubDeptListAsync(deptId);
            }
            finally
            {
                semaphore.Release();
            }

            if (subDepts == null || subDepts.Count == 0)
                return;

            foreach (var dept in subDepts)
                allDepts.Add(dept); // ConcurrentBag 线程安全

            // 所有子部门并发递归
            var tasks = subDepts.Select(dept =>
                TraverseDeptListAsync(dept.DeptId, allDepts, semaphore));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 获取指定父部门下的子部门列表（加 CancellationToken，其余逻辑不变）
        /// </summary>
        public async Task<List<DingTalkDeptInfo>> GetSubDeptListAsync(
            long? deptId = null, CancellationToken ct = default)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{DeptListSubApiUrl}?access_token={corpAccessToken}";
            var bodyObj = new Dictionary<string, object> { { "language", "zh_CN" } };
            if (deptId.HasValue)
                bodyObj["dept_id"] = deptId.Value;

            var content = new StringContent(
                JsonHelper.Serialize(bodyObj),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content, ct); // ← 加 ct
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetSubDeptList deptId={deptId} 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取子部门列表失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            var errcode = root.GetProperty("errcode").GetInt32();

            // 钉钉限流，抛异常触发上层重试
            if (errcode == 88 || errcode == 90018)
                throw new Exception($"钉钉限流 errcode={errcode}，触发重试");

            if (errcode != 0)
                throw new Exception(
                    $"获取子部门列表失败 errcode={errcode}: {root.GetProperty("errmsg").GetString()}");

            return root.GetProperty("result").EnumerateArray()
                .Select(item => JsonHelper.Deserialize<DingTalkDeptInfo>(item.GetRawText()))
                .Where(d => d is not null)
                .ToList()!;
        }

        public async Task<DingTalkDeptInfo> GetDeptByIdAsync(long deptId)
        {
            var corpAccessToken = await GetValidCorpAccessTokenAsync();
            var url = $"{DeptGetApiUrl}?access_token={corpAccessToken}";

            var content = new StringContent(
                JsonHelper.Serialize(new { dept_id = deptId, language = "zh_CN" }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DingTalk] GetDeptById deptId={deptId} 响应: {resultJson}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取部门详情失败 [{response.StatusCode}]: {resultJson}");

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            var errcode = root.GetProperty("errcode").GetInt32();
            if (errcode != 0)
                throw new Exception($"获取部门详情失败: {root.GetProperty("errmsg").GetString()}");

            return JsonHelper.Deserialize<DingTalkDeptInfo>(root.GetProperty("result").GetRawText())
                ?? throw new Exception("解析部门详情失败");
        }

        /// <summary>
        /// 同步钉钉部门到MES系统（支持多层级，多轮扫描）
        /// </summary>
        public async Task<(int successCount, int skipCount, int failCount)> SyncDeptToMesAsync(IReadOnlyCollection<DingTalkDeptInfo> preloadedDingDepts = null)
        {
            int successCount = 0, skipCount = 0, failCount = 0;

            // 1. 获取钉钉所有部门，包含根部门 deptId=1
            var dingDepts = (preloadedDingDepts ?? await GetAllDeptListWithRootAsync())
                .GroupBy(d => d.DeptId)
                .Select(g => g.First())
                .ToList();
            _logger.LogInformation("钉钉共有部门 {count} 个", dingDepts.Count);

            // 2. 获取MES现有部门
            var existingOrgs = _orgManagerApp.LoadAll().ToList();

            // 3. 找出MES缺少的钉钉部门
            var pendingDepts = dingDepts
                .Where(d => !existingOrgs.Any(o => o.Id == d.DeptId.ToString()))
                .ToList();

            if (!pendingDepts.Any())
            {
                _logger.LogInformation("钉钉部门与MES一致，无需同步");
                return (0, dingDepts.Count, 0);
            }

            _logger.LogInformation("发现 {count} 个部门需要同步", pendingDepts.Count);

            // 4. 多轮扫描，每轮处理能找到父部门的节点
            int maxRounds = 20;
            int round = 0;

            while (pendingDepts.Any() && round < maxRounds)
            {
                round++;
                int addedThisRound = 0;
                var stillPending = new List<DingTalkDeptInfo>();

                // 每轮重新加载MES部门（含上一轮新增的）
                existingOrgs = _orgManagerApp.LoadAll().ToList();

                foreach (var dingDept in pendingDepts)
                {
                    try
                    {
                        string parentId = null;
                        string parentName = "根节点";

                        if (dingDept.DeptId != 1)
                        {
                            parentId = dingDept.ParentId > 0 ? dingDept.ParentId.ToString() : null;
                            var parentOrg = existingOrgs.FirstOrDefault(o => o.Id == parentId);

                            if (parentOrg == null)
                            {
                                stillPending.Add(dingDept);
                                continue;
                            }

                            parentName = parentOrg.Name;
                        }

                        var newOrg = new SysOrg
                        {
                            Name       = dingDept.Name ?? string.Empty,
                            ParentId   = parentId,
                            ParentName = parentName,
                            Status     = 0,
                            CascadeId  = string.Empty, // CaculateCascade 自动填
                            HotKey     = string.Empty,
                            IconName   = string.Empty,
                            Id    = dingDept.DeptId.ToString(),
                            CustomCode = string.Empty,
                            TypeName   = string.Empty,
                            TypeId     = string.Empty,
                            SortNo     = 0,
                        };

                        var newId = _orgManagerApp.AddOrgFromJob(newOrg);

                        if (newId == null)
                        {
                            _logger.LogWarning(
                                "[第{round}轮] 「{name}」防重检测已存在，跳过",
                                round, dingDept.Name);
                            skipCount++;
                            continue;
                        }

                        successCount++;
                        addedThisRound++;
                        _logger.LogInformation(
                            "[第{round}轮] 新增部门：「{name}」→ 父：「{parent}」",
                            round, dingDept.Name, parentName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "[第{round}轮] 新增部门「{name}」异常", round, dingDept.Name);
                        failCount++;
                    }
                }

                pendingDepts = stillPending;
                _logger.LogInformation(
                    "[第{round}轮结束] 本轮新增:{added} 剩余:{left}",
                    round, addedThisRound, pendingDepts.Count);

                // 本轮一个都没加进去，说明剩余全部因父部门缺失卡住
                if (addedThisRound == 0 && pendingDepts.Any())
                {
                    foreach (var stuck in pendingDepts)
                    {
                        _logger.LogWarning(
                            "部门「{name}」父部门始终不存在于MES，最终放弃", stuck.Name);
                        failCount++;
                    }
                    break;
                }
            }

            skipCount = dingDepts.Count - successCount - failCount;
            _logger.LogInformation(
                "=== 部门同步完成：新增{s} 跳过{k} 失败{f} 共{round}轮 ===",
                successCount, skipCount, failCount, round);

            return (successCount, skipCount, failCount);
        }

        public async Task<(
            int deptSuccessCount,
            int deptSkipCount,
            int deptFailCount,
            int userSuccessCount,
            int userSkipCount,
            int userFailCount)> SyncContactsToMesAsync()
        {
            var dingDepts = await GetAllDeptListWithRootAsync();

            var (deptSuccessCount, deptSkipCount, deptFailCount) = await SyncDeptToMesAsync(dingDepts);
            var (userSuccessCount, userSkipCount, userFailCount) = await SyncUsersToMesAsync(dingDepts);

            return (deptSuccessCount, deptSkipCount, deptFailCount, userSuccessCount, userSkipCount, userFailCount);
        }

        /// <summary>
        /// 同步钉钉员工到MES系统
        /// </summary>
        public async Task<(int successCount, int skipCount, int failCount)> SyncUsersToMesAsync(IReadOnlyCollection<DingTalkDeptInfo> preloadedDingDepts = null)
        {
            int successCount = 0, skipCount = 0, failCount = 0;

            // 1. 获取钉钉所有部门，定时任务会复用部门同步已拉取的列表，避免重复请求
            var dingDepts = (preloadedDingDepts ?? await GetAllDeptListWithRootAsync())
                .GroupBy(d => d.DeptId)
                .Select(g => g.First())
                .ToList();
            _logger.LogInformation("钉钉共有部门 {count} 个", dingDepts.Count);

            // 2. 获取钉钉所有员工（按部门遍历，userId去重）
            var userMap = new ConcurrentDictionary<string, DingTalkUserDetailInfo>();
            var userFetchSemaphore = new SemaphoreSlim(5);
            var userFetchTasks = dingDepts.Select(async dept =>
            {
                await userFetchSemaphore.WaitAsync();
                try
                {
                    var deptUsers = await GetDeptUserListAsync(dept.DeptId);
                    foreach (var u in deptUsers)
                    {
                        if (!string.IsNullOrEmpty(u.UserId))
                        {
                            userMap.TryAdd(u.UserId, u);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取部门「{name}」员工失败", dept.Name);
                }
                finally
                {
                    userFetchSemaphore.Release();
                }
            });
            await Task.WhenAll(userFetchTasks);
            _logger.LogInformation("钉钉共有员工 {count} 人", userMap.Count);

            // 3. 获取MES现有用户（直接查库，用于判断重复）
            var existingUserList = _sqlSugarClient.Queryable<SysUser>()
                .Where(u => u.Status != -1)
                .Select(u => new { u.Account, u.Id })
                .ToList();

            // 4. 获取MES现有部门（用于匹配 organizationIds）
            var existingOrgs = _orgManagerApp.LoadAll().ToList();

            // 5. 逐个同步员工
            foreach (var kv in userMap)
            {
                var dingUser = kv.Value;
                try
                {
                    // account = jobNumber_name（和前端保持一致）
                    var account = $"{dingUser.JobNumber ?? ""}_{dingUser.Name ?? ""}";

                    // 和前端手动同步保持一致：优先按钉钉 userId 判断是否已存在
                    if (!string.IsNullOrEmpty(dingUser.UserId)
                        && existingUserList.Any(u => u.Id == dingUser.UserId))
                    {
                        _logger.LogInformation("账号「{account}」已存在，跳过", account);
                        skipCount++;
                        continue;
                    }

                    if (existingUserList.Any(u => u.Account == account))
                    {
                        _logger.LogWarning("账号「{account}」已存在但Id不是钉钉userId，跳过", account);
                        skipCount++;
                        continue;
                    }

                    // 匹配部门：MES部门Id已经使用钉钉deptId，直接按Id匹配
                    var orgIdArr = new List<string>();
                    if (dingUser.DeptIdList != null && dingUser.DeptIdList.Count > 0)
                    {
                        foreach (var dingDeptId in dingUser.DeptIdList)
                        {
                            var sysOrg = existingOrgs.FirstOrDefault(o => o.Id == dingDeptId.ToString());
                            if (sysOrg != null)
                            {
                                orgIdArr.Add(sysOrg.Id);
                            }
                        }
                    }

                    // 没有匹配到任何部门则跳过
                    if (!orgIdArr.Any())
                    {
                        _logger.LogWarning("「{name}」未匹配到MES部门，跳过", dingUser.Name);
                        failCount++;
                        continue;
                    }

                    var organizationIds = string.Join(",", orgIdArr.Distinct());

                    // 构造新用户请求
                    var newUserReq = new UpdateUserReq
                    {
                        Account         = account,
                        Name            = dingUser.Name    ?? "",
                        Password        = account,
                        OrganizationIds = organizationIds,
                        Id         = dingUser.UserId  ?? "",
                        Status          = 1,
                        Sex             = 0,
                        ParentId        =  string.Empty,
                    };

                    // 复用已有的注册方法，绕过登录上下文
                    _userManagerApp.LoginOrRegisterAccountFromDingTalk(
                        newUserReq,
                        createId: "49df1602-f5f3-4d52-afb7-3802da619558",
                        enableRegister: true
                    );

                    successCount++;
                    _logger.LogInformation("新增员工：「{name}」account={account} 部门={org}",
                        dingUser.Name, account, organizationIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "同步员工「{name}」失败", dingUser.Name);
                    failCount++;
                }
            }

            _logger.LogInformation("=== 员工同步完成：新增{s} 跳过{k} 失败{f} ===",
                successCount, skipCount, failCount);

            return (successCount, skipCount, failCount);
        }
    }
}
