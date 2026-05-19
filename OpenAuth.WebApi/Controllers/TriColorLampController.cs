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

        /// <summary>
        /// 根据组织名称获取灯设备列表
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampDeviceResponse>>> GetUserGroupDtuSns([FromQuery] string groupName)
        {
            var result = new Response<List<TriColorLampDeviceResponse>>();
            try
            {
                result.Data = await _app.GetUserGroupDtuSnsAsync(groupName);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn 获取灯数据
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampDataResponse>>> GetDtuSnData(
            [FromQuery] string dtuSn,
            [FromQuery] string date = null)
        {
            var result = new Response<List<TriColorLampDataResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnDataAsync(dtuSn, date);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSnList 获取灯数据
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampDataResponse>>> GetDtuSnListData(
            [FromQuery] string dtuSns,
            [FromQuery] string date = null)
        {
            var result = new Response<List<TriColorLampDataResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnListDataAsync(dtuSns, date);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn 获取灯状态
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampCurrentStateResponse>>> GetDtuSnState([FromQuery] string dtuSn)
        {
            var result = new Response<List<TriColorLampCurrentStateResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnStateAsync(dtuSn);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSnList 获取灯状态
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<List<TriColorLampCurrentStateResponse>>>> GetDtuSnStateList([FromQuery] string dtuSns)
        {
            var result = new Response<List<List<TriColorLampCurrentStateResponse>>>();
            try
            {
                result.Data = await _app.GetDtuSnStateListAsync(dtuSns);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn 及日期获取稼动率数据集
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampRateResponse>>> GetDtuSnRateOfAction(
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] string dtuSn)
        {
            var result = new Response<List<TriColorLampRateResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnRateOfActionAsync(startDate, endDate, dtuSn);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSnList 及日期获取稼动率数据集
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampRateResponse>>> GetDtuSnListRateOfAction(
            [FromQuery] string date,
            [FromQuery] string dtuSns)
        {
            var result = new Response<List<TriColorLampRateResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnListRateOfActionAsync(date, dtuSns);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn 获取当日每种灯的次数
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampStateCountResponse>>> GetDtuSnStateCount([FromQuery] string dtuSn)
        {
            var result = new Response<List<TriColorLampStateCountResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnStateCountAsync(dtuSn);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSnList 获取当日每种灯的次数
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampStateCountResponse>>> GetDtuSnListStateCount([FromQuery] string dtuSns)
        {
            var result = new Response<List<TriColorLampStateCountResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnListStateCountAsync(dtuSns);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn 序列号，下发蓝灯
        /// </summary>
        [HttpPut]
        [AllowAnonymous]
        public async Task<Response<TriColorLampCommandResponse>> SendBlueLightCommand(
            [FromQuery] string dtuSn,
            [FromQuery] string tag = "HJ",
            [FromQuery] int sendValue = 2)
        {
            var result = new Response<TriColorLampCommandResponse>();
            try
            {
                result.Data = await _app.SendBlueLightCommandAsync(dtuSn, tag, sendValue);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn，获取当前用户下计数最新数
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampCounterLatestResponse>>> GetDtuSnCounter([FromQuery] string dtuSn)
        {
            var result = new Response<List<TriColorLampCounterLatestResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnCounterAsync(dtuSn);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSnList，获取当前用户下计数最新数
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampCounterLatestResponse>>> GetDtuSnListCounter([FromQuery] string dtuSns)
        {
            var result = new Response<List<TriColorLampCounterLatestResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnListCounterAsync(dtuSns);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据日期，获取当前用户下 dtuSn 计数历史数据
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampCounterHistoryResponse>>> GetDtuSnHistoryData(
            [FromQuery] string dtuSn,
            [FromQuery] string date)
        {
            var result = new Response<List<TriColorLampCounterHistoryResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnHistoryDataAsync(dtuSn, date);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据日期，获取当前用户下 dtuSnList 计数历史数据
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampCounterHistoryResponse>>> GetDtuSnListHistoryData(
            [FromQuery] string dtuSns,
            [FromQuery] string date)
        {
            var result = new Response<List<TriColorLampCounterHistoryResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnListHistoryDataAsync(dtuSns, date);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn 序列号，下发计数清零
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<Response<TriColorLampCommandResponse>> SendCounterClearCommand(
            [FromQuery] string dtuSn,
            [FromQuery] string tag = "JS",
            [FromQuery] int value = 0)
        {
            var result = new Response<TriColorLampCommandResponse>();
            try
            {
                result.Data = await _app.SendCounterClearCommandAsync(dtuSn, tag, value);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 根据 dtuSn 序列号，查询天计数统计
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<Response<List<TriColorLampCounterDayHistoryResponse>>> GetDtuSnDayHistoryData(
            [FromQuery] string dtuSn,
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            var result = new Response<List<TriColorLampCounterDayHistoryResponse>>();
            try
            {
                result.Data = await _app.GetDtuSnDayHistoryDataAsync(dtuSn, startDate, endDate);
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
