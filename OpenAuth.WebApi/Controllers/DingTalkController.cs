using global::OpenAuth.App.DingTalk;
using global::OpenAuth.App.DingTalk.Request;
using global::OpenAuth.App.DingTalk.Response;
using global::OpenAuth.Repository.Domain.DingTalk;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAuth.App;
using OpenAuth.App.Response;
using OpenAuth.App.SyncTaskManager;
using OpenAuth.Repository.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAuth.WebApi.Controllers
{
    /// <summary>
    /// 钉钉
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "钉钉_DingTalk")]
    public class DingTalkController : ControllerBase
    {
        private readonly DingTalkApp _app;
        private readonly SyncTaskApp _syncTaskApp;

        public DingTalkController(DingTalkApp app,SyncTaskApp syncTaskApp)
        {
            _app = app;
            _syncTaskApp = syncTaskApp;
        }

        // ─────────────────────────────────────────────
        // 用户授权码模式（原有接口）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 通过授权码获取用户信息
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkUserInfo>> GetUserByAuthCode([FromQuery] string authCode)
        {
            var result = new Response<DingTalkUserInfo>();
            try
            {
                result.Data = await _app.GetUserByAuthCodeAsync(authCode);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取钉钉配置信息（ClientId、CorpId，不含 ClientSecret）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public Response<DingTalkOptions> GetDingTalkOptions()
        {
            var result = new Response<DingTalkOptions>();
            try
            {
                result.Data = _app.GetDingTalkOptions();
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 发送钉钉工作通知，msg按钉钉接口原始格式传入
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<Response<DingTalkWorkNotificationResponse>> SendWorkNotification([FromBody] DingTalkWorkNotificationReq req)
        {
            var result = new Response<DingTalkWorkNotificationResponse>();
            try
            {
                result.Data = await _app.SendWorkNotificationAsync(req);
                result.Message = "发送工作通知成功";
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 发送钉钉文本工作通知
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<Response<DingTalkWorkNotificationResponse>> SendTextWorkNotification([FromBody] DingTalkTextWorkNotificationReq req)
        {
            var result = new Response<DingTalkWorkNotificationResponse>();
            try
            {
                result.Data = await _app.SendTextWorkNotificationAsync(req);
                result.Message = "发送文本工作通知成功";
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过授权码获取用户级 AccessToken
        /// </summary>
        [HttpGet]
        public async Task<Response<string>> GetAccessToken([FromQuery] string authCode)
        {
            var result = new Response<string>();
            try
            {
                result.Data = await _app.GetAccessTokenAsync(authCode);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过用户级 AccessToken 获取用户信息
        /// </summary>
        [HttpGet]
        public async Task<Response<DingTalkUserInfo>> GetUserInfo([FromQuery] string accessToken)
        {
            var result = new Response<DingTalkUserInfo>();
            try
            {
                result.Data = await _app.GetUserInfoAsync(accessToken);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        // ─────────────────────────────────────────────
        // 企业内部应用模式（新增接口）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 通过免登授权码获取企业用户信息（企业内部应用模式）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkUserDetailInfo>> GetUserByCorpCode([FromQuery] string code)
        {
            var result = new Response<DingTalkUserDetailInfo>();
            try
            {
                result.Data = await _app.GetUserDetailInfoByCorpCodeAsync(code);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过 corpId 获取企业级 AccessToken（client_credentials 模式）
        /// </summary>
        [HttpGet]
        public async Task<Response<string>> GetCorpAccessToken([FromQuery] string corpId)
        {
            var result = new Response<string>();
            try
            {
                var (token, _) = await _app.GetCorpAccessTokenAsync(corpId);
                result.Data = token;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过 userId 获取用户详细信息
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkUserDetailInfo>> GetUserByUserId([FromQuery] string userId)
        {
            var result = new Response<DingTalkUserDetailInfo>();
            try
            {
                result.Data = await _app.GetUserDetailInfoByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过授权码获取用户详细信息
        /// <param name="authCode">钉钉授权码</param>
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkUserDetailInfo>> GetUserDetailInfoByAuthCode([FromQuery] string authCode)
        {
            var result = new Response<DingTalkUserDetailInfo>();
            try
            {
                result.Data = await _app.GetUserDetailInfoByAuthCodeAsync(authCode);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过 unionId 获取用户 userId
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<string>> GetUserIdByUnionId([FromQuery] string unionId)
        {
            var result = new Response<string>();
            try
            {
                result.Data = await _app.GetUserIdByUnionIdAsync(unionId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取指定父部门下的子部门ID列表
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<long>>> GetSubDeptIdList([FromQuery] long deptId)
        {
            var result = new Response<List<long>>();
            try
            {
                result.Data = await _app.GetSubDeptIdListAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 递归获取企业下所有部门ID列表（从根部门开始）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<long>>> GetAllDeptIdList()
        {
            var result = new Response<List<long>>();
            try
            {
                var allDeptIds = new List<long>();
                await _app.TraverseDeptAsync(1, allDeptIds);
                result.Data = allDeptIds;
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取指定部门的员工列表
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<DingTalkUserDetailInfo>>> GetDeptUserList([FromQuery] long deptId)
        {
            var result = new Response<List<DingTalkUserDetailInfo>>();
            try
            {
                result.Data = await _app.GetDeptUserListAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取企业所有员工列表（遍历全部部门，自动去重）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<DingTalkUserDetailInfo>>> GetAllUsers()
        {
            var result = new Response<List<DingTalkUserDetailInfo>>();
            try
            {
                result.Data = await _app.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取指定父部门下的子部门列表（含部门详情），不传deptId默认从根部门开始
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<DingTalkDeptInfo>>> GetSubDeptList([FromQuery] long? deptId = null)
        {
            var result = new Response<List<DingTalkDeptInfo>>();
            try
            {
                result.Data = await _app.GetSubDeptListAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 触发异步同步任务，立即返回 taskId
        /// </summary>
        [HttpPost]
        public IActionResult StartSyncAllDeptList()
        {
            var taskId = _syncTaskApp.CreateTask();

            _ = Task.Run(async () =>
            {
                try
                {
                    var depts = await _app.GetAllDeptListAsync();
                    // TODO: 持久化到数据库（如有需要）
                    _syncTaskApp.SetSuccess(taskId, depts.Count);
                }
                catch (Exception ex)
                {
                    _syncTaskApp.SetFailed(taskId, ex.Message);
                }
            });

            var result = new Response<string>();
            result.Data = taskId;
            result.Message = "同步任务已启动";
            return Ok(result);
        }

        /// <summary>
        /// 轮询同步任务状态
        /// </summary>
        [HttpGet]
        public IActionResult GetSyncDeptStatus([FromQuery] string taskId)
        {
            var result = new Response<SyncTaskState>();
            var task = _syncTaskApp.Get(taskId);
            if (task == null)
            {
                result.Code = 404;
                result.Message = "任务不存在";
                return Ok(result);
            }
            result.Data = task;
            return Ok(result);
        }

        /// <summary>
        /// 递归获取企业所有部门列表（含详情）
        /// 保留原接口，但建议部门数量多时改用上面的异步方式
        /// </summary>
        [HttpGet]
        public async Task<Response<List<DingTalkDeptInfo>>> GetAllDeptList()
        {
            var result = new Response<List<DingTalkDeptInfo>>();
            try
            {
                result.Data = await _app.GetAllDeptListAsync();
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过部门ID获取部门详细信息
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<DingTalkDeptInfo>> GetDeptById([FromQuery] long deptId)
        {
            var result = new Response<DingTalkDeptInfo>();
            try
            {
                result.Data = await _app.GetDeptByIdAsync(deptId);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
