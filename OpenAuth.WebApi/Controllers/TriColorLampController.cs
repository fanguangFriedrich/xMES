using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAuth.App.TriColorLamp;
using System;
using System.Threading.Tasks;

namespace OpenAuth.WebApi.Controllers
{
    /// <summary>
    /// 三色灯
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "三色灯_TriColorLamp")]
    public class TriColorLampController : ControllerBase
    {
        private readonly TriColorLampApp _app;

        public TriColorLampController(TriColorLampApp app)
        {
            _app = app;
        }

        /// <summary>
        /// 获取三色灯 Token
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<string>> GetAccessToken()
        {
            var result = new Response<string>();
            try
            {
                result.Data = await _app.GetValidAccessTokenAsync();
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
