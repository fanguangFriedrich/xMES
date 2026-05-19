using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAuth.App.TriColorLamp;
using OpenAuth.App.TriColorLamp.Response;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 根据日期查询当日的正常数、离线数及告警数
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<TriColorLampStateInfoResponse>> GetStateInfo([FromQuery] string date)
        {
            var result = new Response<TriColorLampStateInfoResponse>();
            try
            {
                result.Data = await _app.GetStateInfoAsync(date);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据灯状态，获取当日灯状态排名
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampRankingResponse>>> GetRanking(
            [FromQuery] int state,
            [FromQuery] int ranking)
        {
            var result = new Response<List<TriColorLampRankingResponse>>();
            try
            {
                result.Data = await _app.GetRankingAsync(state, ranking);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取用户下灯设备列表
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampDeviceResponse>>> GetUserDtuSns()
        {
            var result = new Response<List<TriColorLampDeviceResponse>>();
            try
            {
                result.Data = await _app.GetUserDtuSnsAsync();
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
