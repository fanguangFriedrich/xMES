using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAuth.App.DingTalk;
using OpenAuth.App.Response;
using System;
using System.Threading.Tasks;

namespace OpenAuth.WebApi.Controllers
{
    /// <summary>
    /// 钉钉登录
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "钉钉登录_DingTalkLogin")]
    public class DingTalkLoginController : ControllerBase
    {
        private readonly DingTalkLoginApp _dingTalkLoginApp;
        private readonly DingTalkApp _dingTalkApp;

        public DingTalkLoginController(DingTalkLoginApp dingTalkLoginApp, DingTalkApp dingTalkApp)
        {
            _dingTalkLoginApp = dingTalkLoginApp;
            _dingTalkApp = dingTalkApp;
        }

        /// <summary>
        /// 通过钉钉免登授权码登录（钉钉端内免密登录）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<UserView>> LoginByDingTalkApp([FromQuery] string code)
        {
            var result = new Response<UserView>();
            try
            {
                // 1. 公司码 → 用户详细信息（userId）
                var userDetail = await _dingTalkApp.GetUserDetailInfoByCorpCodeAsync(code);
                // 2. 登录或注册
                result.Data = await _dingTalkLoginApp.LoginOrRegisterByDingTalkAsync(userDetail);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 通过 authCode 登录（网页扫码登录回调）
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<UserView>> LoginByScanCode([FromQuery] string authCode)
        {
            var result = new Response<UserView>();
            try
            {
                // 1. authCode → 用户详细信息
                var userDetail = await _dingTalkApp.GetUserDetailInfoByAuthCodeAsync(authCode);
                // 2. 登录或注册
                result.Data = await _dingTalkLoginApp.LoginOrRegisterByDingTalkAsync(userDetail);
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
