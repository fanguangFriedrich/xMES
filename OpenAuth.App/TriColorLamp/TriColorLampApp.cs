using Infrastructure;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAuth.App.TriColorLamp.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAuth.App.TriColorLamp
{
    public class TriColorLampApp
    {
        private readonly HttpClient _httpClient;
        private readonly TriColorLampOptions _options;
        private readonly ILogger<TriColorLampApp> _logger;

        private static readonly SemaphoreSlim TokenRefreshLock = new(1, 1);

        private string _cachedAccessToken;
        private DateTime _accessTokenExpiry = DateTime.MinValue;

        private const string TokenApiPath = "auth/token";
        private const string StateInfoApiPath = "triColorLamp/stateInfo";
        private const string RankingApiPath = "triColorLamp/ranking";
        private const string UserDtuSnsApiPath = "triColorLamp/userDtuSns";
        private const string UserGroupDtuSnsApiPath = "triColorLamp/userGroupDtuSns";
        private const string DtuSnApiPath = "triColorLamp/dtuSn";
        private const string DtuSnListApiPath = "triColorLamp/dtuSnList";
        private const string DtuSnStateApiPath = "triColorLamp/dtuSnState";
        private const string DtuSnStateListApiPath = "triColorLamp/dtuSnStateList";
        private const string DtuSnRateOfActionApiPath = "triColorLamp/dtuSnRateOfAction";
        private const string DtuSnListRateOfActionApiPath = "triColorLamp/dtuSnListRateOfAction";
        private const string DtuSnStateCountApiPath = "triColorLamp/dtuSnStateCount";
        private const string DtuSnListStateCountApiPath = "triColorLamp/dtuSnListStateCount";
        private const string SendBlueLightCmdApiPath = "triColorLamp/sendCmds";
        private const string DtuSnCounterApiPath = "api/counter/dtuSnCounter";
        private const string DtuSnListCounterApiPath = "api/counter/dtuSnListCounter";
        private const string DtuSnHistoryDataApiPath = "api/counter/dtuSnHistoryData";
        private const string DtuSnListHistoryDataApiPath = "api/counter/dtuSnListHistoryData";
        private const string SendCounterClearCmdApiPath = "api/counter/sendCmds";
        private const string DtuSnDayHistoryDataApiPath = "api/counter/dtuSnDayHistoryData";
        private const int TokenExpireSeconds = 2 * 60 * 60;

        public TriColorLampApp(
            HttpClient httpClient,
            IOptions<TriColorLampOptions> options,
            ILogger<TriColorLampApp> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GetValidAccessTokenAsync()
        {
            if (_cachedAccessToken != null && DateTime.UtcNow < _accessTokenExpiry.AddMinutes(-5))
            {
                return _cachedAccessToken;
            }

            await TokenRefreshLock.WaitAsync();
            try
            {
                if (_cachedAccessToken != null && DateTime.UtcNow < _accessTokenExpiry.AddMinutes(-5))
                {
                    return _cachedAccessToken;
                }

                var cachedToken = LoadPersistedAccessToken(false);
                if (cachedToken != null)
                {
                    _cachedAccessToken = cachedToken.Token;
                    _accessTokenExpiry = cachedToken.Expiry;
                    _logger.LogInformation("[TriColorLamp] AccessToken loaded from local cache, expires at {Expiry:u}", _accessTokenExpiry);
                    return _cachedAccessToken;
                }

                try
                {
                    (_cachedAccessToken, _accessTokenExpiry) = await GetAccessTokenAsync();
                    SavePersistedAccessToken(_cachedAccessToken, _accessTokenExpiry);
                    _logger.LogInformation("[TriColorLamp] AccessToken refreshed, expires at {Expiry:u}", _accessTokenExpiry);
                }
                catch (Exception ex)
                {
                    var fallbackToken = LoadPersistedAccessToken(true);
                    if (fallbackToken == null)
                        throw;

                    _cachedAccessToken = fallbackToken.Token;
                    _accessTokenExpiry = fallbackToken.Expiry > DateTime.UtcNow.AddMinutes(5)
                        ? fallbackToken.Expiry
                        : DateTime.UtcNow.AddMinutes(30);
                    SavePersistedAccessToken(_cachedAccessToken, _accessTokenExpiry);
                    _logger.LogWarning(ex, "[TriColorLamp] Token refresh failed, fallback to local cached AccessToken. Cached expiry: {Expiry:u}", _accessTokenExpiry);
                }
            }
            finally
            {
                TokenRefreshLock.Release();
            }

            return _cachedAccessToken;
        }

        public async Task<(string Token, DateTime Expiry)> GetAccessTokenAsync()
        {
            ValidateOptions();

            var body = new
            {
                username = _options.Username,
                password = _options.Password
            };

            var content = new StringContent(
                JsonHelper.SerializeCamelCase(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(BuildApiUrl(TokenApiPath), content);
            var resultJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("[TriColorLamp] Token response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Get tri-color lamp token failed [{response.StatusCode}]: {resultJson}");

            var result = JsonHelper.Deserialize<TriColorLampTokenResponse>(resultJson)
                ?? throw new Exception("Parse tri-color lamp token response failed");

            if (result.Code != 200)
                throw new Exception($"Get tri-color lamp token failed code={result.Code}: {result.Msg}");

            var token = result.Data == null ? null : result.Data.Token;
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception($"Token response does not contain data.token: {resultJson}");

            return (token, DateTime.UtcNow.AddSeconds(TokenExpireSeconds));
        }

        public Task<TriColorLampStateInfoResponse> GetStateInfoAsync(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("date cannot be empty", nameof(date));

            var url = BuildApiUrl($"{StateInfoApiPath}?date={Uri.EscapeDataString(date)}");
            return GetApiDataAsync<TriColorLampStateInfoResponse>(url, "Get tri-color lamp state info failed");
        }

        public Task<List<TriColorLampRankingResponse>> GetRankingAsync(int state, int ranking)
        {
            if (ranking <= 0)
                throw new ArgumentException("ranking must be greater than 0", nameof(ranking));

            var url = BuildApiUrl($"{RankingApiPath}?state={state}&ranking={ranking}");
            return GetApiDataAsync<List<TriColorLampRankingResponse>>(url, "Get tri-color lamp ranking failed");
        }

        public Task<List<TriColorLampDeviceResponse>> GetUserDtuSnsAsync()
        {
            return GetApiDataAsync<List<TriColorLampDeviceResponse>>(
                BuildApiUrl(UserDtuSnsApiPath),
                "Get tri-color lamp device list failed");
        }

        public Task<List<TriColorLampDeviceResponse>> GetUserGroupDtuSnsAsync(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("groupName cannot be empty", nameof(groupName));

            var url = BuildApiUrl(BuildQuery(UserGroupDtuSnsApiPath, ("groupName", groupName)));
            return GetApiDataAsync<List<TriColorLampDeviceResponse>>(url, "Get tri-color lamp group device list failed");
        }

        public Task<List<TriColorLampDataResponse>> GetDtuSnDataAsync(string dtuSn, string date = null)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            var url = BuildApiUrl(BuildQuery(DtuSnApiPath, ("dtuSn", dtuSn), ("date", date)));
            return GetApiDataAsync<List<TriColorLampDataResponse>>(url, "Get tri-color lamp data failed");
        }

        public Task<List<TriColorLampDataResponse>> GetDtuSnListDataAsync(string dtuSns, string date = null)
        {
            if (string.IsNullOrWhiteSpace(dtuSns))
                throw new ArgumentException("dtuSns cannot be empty", nameof(dtuSns));

            var url = BuildApiUrl(BuildQuery(DtuSnListApiPath, ("dtuSns", dtuSns), ("date", date)));
            return GetApiDataAsync<List<TriColorLampDataResponse>>(url, "Get tri-color lamp list data failed");
        }

        public Task<List<TriColorLampCurrentStateResponse>> GetDtuSnStateAsync(string dtuSn)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            var url = BuildApiUrl(BuildQuery(DtuSnStateApiPath, ("dtuSn", dtuSn)));
            return GetApiDataAsync<List<TriColorLampCurrentStateResponse>>(url, "Get tri-color lamp state failed");
        }

        public Task<List<List<TriColorLampCurrentStateResponse>>> GetDtuSnStateListAsync(string dtuSns)
        {
            if (string.IsNullOrWhiteSpace(dtuSns))
                throw new ArgumentException("dtuSns cannot be empty", nameof(dtuSns));

            var url = BuildApiUrl(BuildQuery(DtuSnStateListApiPath, ("dtuSns", dtuSns)));
            return GetApiDataAsync<List<List<TriColorLampCurrentStateResponse>>>(url, "Get tri-color lamp state list failed");
        }

        public Task<List<TriColorLampRateResponse>> GetDtuSnRateOfActionAsync(string startDate, string endDate, string dtuSn)
        {
            if (string.IsNullOrWhiteSpace(startDate))
                throw new ArgumentException("startDate cannot be empty", nameof(startDate));

            if (string.IsNullOrWhiteSpace(endDate))
                throw new ArgumentException("endDate cannot be empty", nameof(endDate));

            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            var url = BuildApiUrl(BuildQuery(DtuSnRateOfActionApiPath, ("startDate", startDate), ("endDate", endDate), ("dtuSn", dtuSn)));
            return GetApiDataAsync<List<TriColorLampRateResponse>>(url, "Get tri-color lamp rate of action failed");
        }

        public Task<List<TriColorLampRateResponse>> GetDtuSnListRateOfActionAsync(string date, string dtuSns)
        {
            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("date cannot be empty", nameof(date));

            if (string.IsNullOrWhiteSpace(dtuSns))
                throw new ArgumentException("dtuSns cannot be empty", nameof(dtuSns));

            var url = BuildApiUrl(BuildQuery(DtuSnListRateOfActionApiPath, ("date", date), ("dtuSns", dtuSns)));
            return GetApiDataAsync<List<TriColorLampRateResponse>>(url, "Get tri-color lamp list rate of action failed");
        }

        public Task<List<TriColorLampStateCountResponse>> GetDtuSnStateCountAsync(string dtuSn)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            var url = BuildApiUrl(BuildQuery(DtuSnStateCountApiPath, ("dtuSn", dtuSn)));
            return GetApiDataAsync<List<TriColorLampStateCountResponse>>(url, "Get tri-color lamp state count failed");
        }

        public Task<List<TriColorLampStateCountResponse>> GetDtuSnListStateCountAsync(string dtuSns)
        {
            if (string.IsNullOrWhiteSpace(dtuSns))
                throw new ArgumentException("dtuSns cannot be empty", nameof(dtuSns));

            var url = BuildApiUrl(BuildQuery(DtuSnListStateCountApiPath, ("dtuSns", dtuSns)));
            return GetApiDataAsync<List<TriColorLampStateCountResponse>>(url, "Get tri-color lamp list state count failed");
        }

        public Task<TriColorLampCommandResponse> SendBlueLightCommandAsync(string dtuSn, string tag = "HJ", int sendValue = 2)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("tag cannot be empty", nameof(tag));

            var body = new
            {
                dtuSn,
                tag,
                sendValue
            };

            return SendApiAsync(BuildApiUrl(SendBlueLightCmdApiPath), HttpMethod.Put, body, "Send tri-color lamp blue light command failed");
        }

        public Task<List<TriColorLampCounterLatestResponse>> GetDtuSnCounterAsync(string dtuSn)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            var url = BuildApiUrl(BuildQuery(DtuSnCounterApiPath, ("dtuSn", dtuSn)));
            return GetApiDataAsync<List<TriColorLampCounterLatestResponse>>(url, "Get tri-color lamp counter latest data failed");
        }

        public Task<List<TriColorLampCounterLatestResponse>> GetDtuSnListCounterAsync(string dtuSns)
        {
            if (string.IsNullOrWhiteSpace(dtuSns))
                throw new ArgumentException("dtuSns cannot be empty", nameof(dtuSns));

            var url = BuildApiUrl(BuildQuery(DtuSnListCounterApiPath, ("dtuSns", dtuSns)));
            return GetApiDataAsync<List<TriColorLampCounterLatestResponse>>(url, "Get tri-color lamp counter list latest data failed");
        }

        public Task<List<TriColorLampCounterHistoryResponse>> GetDtuSnHistoryDataAsync(string dtuSn, string date)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("date cannot be empty", nameof(date));

            var url = BuildApiUrl(BuildQuery(DtuSnHistoryDataApiPath, ("dtuSn", dtuSn), ("date", date)));
            return GetApiDataAsync<List<TriColorLampCounterHistoryResponse>>(url, "Get tri-color lamp counter history data failed");
        }

        public Task<List<TriColorLampCounterHistoryResponse>> GetDtuSnListHistoryDataAsync(string dtuSns, string date)
        {
            if (string.IsNullOrWhiteSpace(dtuSns))
                throw new ArgumentException("dtuSns cannot be empty", nameof(dtuSns));

            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("date cannot be empty", nameof(date));

            var url = BuildApiUrl(BuildQuery(DtuSnListHistoryDataApiPath, ("dtuSns", dtuSns), ("date", date)));
            return GetApiDataAsync<List<TriColorLampCounterHistoryResponse>>(url, "Get tri-color lamp counter list history data failed");
        }

        public Task<TriColorLampCommandResponse> SendCounterClearCommandAsync(string dtuSn, string tag = "JS", int value = 0)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("tag cannot be empty", nameof(tag));

            var body = new
            {
                dtuSn,
                tag,
                value
            };

            return SendApiAsync(BuildApiUrl(SendCounterClearCmdApiPath), HttpMethod.Post, body, "Send tri-color lamp counter clear command failed");
        }

        public Task<List<TriColorLampCounterDayHistoryResponse>> GetDtuSnDayHistoryDataAsync(string dtuSn, string startDate, string endDate)
        {
            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            if (string.IsNullOrWhiteSpace(startDate))
                throw new ArgumentException("startDate cannot be empty", nameof(startDate));

            if (string.IsNullOrWhiteSpace(endDate))
                throw new ArgumentException("endDate cannot be empty", nameof(endDate));

            var url = BuildApiUrl(BuildQuery(DtuSnDayHistoryDataApiPath, ("dtuSn", dtuSn), ("startDate", startDate), ("endDate", endDate)));
            return GetApiDataAsync<List<TriColorLampCounterDayHistoryResponse>>(url, "Get tri-color lamp counter day history data failed");
        }

        public HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
        {
            if (string.IsNullOrWhiteSpace(requestUri))
                throw new ArgumentException("requestUri cannot be empty", nameof(requestUri));

            return new HttpRequestMessage(method, requestUri);
        }

        public async Task AddAuthorizationHeaderAsync(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var token = await GetValidAccessTokenAsync();
            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        private async Task<T> GetApiDataAsync<T>(string url, string errorMessage)
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Get, url);
            await AddAuthorizationHeaderAsync(request);

            var response = await _httpClient.SendAsync(request);
            var resultJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("[TriColorLamp] GET {Url} response status: {StatusCode}", url, response.StatusCode);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"{errorMessage} [{response.StatusCode}]: {resultJson}");

            var result = JsonHelper.Deserialize<TriColorLampApiResponse<T>>(resultJson)
                ?? throw new Exception($"{errorMessage}: parse response failed");

            if (result.Code != 200)
                throw new Exception($"{errorMessage} code={result.Code}: {result.Msg}");

            ExtendPersistedAccessToken();
            return result.Data;
        }

        private async Task<TriColorLampCommandResponse> SendApiAsync(string url, HttpMethod method, object body, string errorMessage)
        {
            using var request = CreateAuthorizedRequest(method, url);
            await AddAuthorizationHeaderAsync(request);
            request.Content = new StringContent(
                JsonHelper.SerializeCamelCase(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            var resultJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("[TriColorLamp] {Method} {Url} response status: {StatusCode}", method, url, response.StatusCode);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"{errorMessage} [{response.StatusCode}]: {resultJson}");

            var result = JsonHelper.Deserialize<TriColorLampCommandResponse>(resultJson)
                ?? throw new Exception($"{errorMessage}: parse response failed");

            if (result.Code != 200)
                throw new Exception($"{errorMessage} code={result.Code}: {result.Msg}");

            ExtendPersistedAccessToken();
            return result;
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new Exception("TriColorLamp BaseUrl is not configured");

            if (string.IsNullOrWhiteSpace(_options.Username))
                throw new Exception("TriColorLamp Username is not configured");

            if (string.IsNullOrWhiteSpace(_options.Password))
                throw new Exception("TriColorLamp Password is not configured");
        }

        private string BuildApiUrl(string apiPath)
        {
            ValidateOptions();

            var baseUrl = _options.BaseUrl.EndsWith("/")
                ? _options.BaseUrl
                : $"{_options.BaseUrl}/";

            return new Uri(new Uri(baseUrl), apiPath).ToString();
        }

        private static string BuildQuery(string apiPath, params (string Name, string Value)[] queryParams)
        {
            var queryItems = new List<string>();
            foreach (var queryParam in queryParams)
            {
                if (string.IsNullOrWhiteSpace(queryParam.Value))
                    continue;

                queryItems.Add($"{Uri.EscapeDataString(queryParam.Name)}={Uri.EscapeDataString(queryParam.Value)}");
            }

            return queryItems.Count == 0
                ? apiPath
                : $"{apiPath}?{string.Join("&", queryItems)}";
        }

        private PersistedTriColorLampToken LoadPersistedAccessToken(bool allowExpired)
        {
            try
            {
                var cachePath = GetTokenCachePath();
                if (!File.Exists(cachePath))
                    return null;

                var cacheJson = File.ReadAllText(cachePath, Encoding.UTF8);
                var cache = JsonHelper.Deserialize<PersistedTriColorLampToken>(cacheJson);
                if (cache == null || string.IsNullOrWhiteSpace(cache.Token))
                    return null;

                if (!string.Equals(cache.BaseUrl, _options.BaseUrl, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(cache.Username, _options.Username, StringComparison.OrdinalIgnoreCase))
                    return null;

                if (!allowExpired && DateTime.UtcNow >= cache.Expiry.AddMinutes(-5))
                    return null;

                return cache;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TriColorLamp] Failed to load local AccessToken cache");
                return null;
            }
        }

        private void SavePersistedAccessToken(string token, DateTime expiry)
        {
            try
            {
                var cachePath = GetTokenCachePath();
                var cacheDirectory = Path.GetDirectoryName(cachePath);
                if (!string.IsNullOrWhiteSpace(cacheDirectory))
                    Directory.CreateDirectory(cacheDirectory);

                var cache = new PersistedTriColorLampToken
                {
                    Token = token,
                    Expiry = expiry,
                    BaseUrl = _options.BaseUrl,
                    Username = _options.Username
                };

                File.WriteAllText(cachePath, JsonHelper.SerializeCamelCase(cache), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TriColorLamp] Failed to save local AccessToken cache");
            }
        }

        private void ExtendPersistedAccessToken()
        {
            if (string.IsNullOrWhiteSpace(_cachedAccessToken))
                return;

            _accessTokenExpiry = DateTime.UtcNow.AddSeconds(TokenExpireSeconds);
            SavePersistedAccessToken(_cachedAccessToken, _accessTokenExpiry);
        }

        private static string GetTokenCachePath()
        {
            return Path.Combine(AppContext.BaseDirectory, ".cache", "tri-color-lamp-token.json");
        }

        private class PersistedTriColorLampToken
        {
            public string Token { get; set; }
            public DateTime Expiry { get; set; }
            public string BaseUrl { get; set; }
            public string Username { get; set; }
        }
    }
}
