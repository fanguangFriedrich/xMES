using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAuth.App;
using OpenAuth.App.Mes.WorkOrder;
using OpenAuth.App.Mes.WorkOrder.Request;
using OpenAuth.App.Request;
using OpenAuth.App.Response;
using OpenAuth.Repository.Domain.Mes;
using System;
using System.Threading.Tasks;

namespace OpenAuth.WebApi.Controllers
{
    /// <summary>
    /// 订单明细
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]   
    [ApiExplorerSettings(GroupName = "派工单明细_MesWorkOrders")]
    public class MesWorkOrderController : ControllerBase
    {
        private readonly MesWorkOrderApp _app;

        public MesWorkOrderController(MesWorkOrderApp app)
        {
            _app = app;
        }

        /// <summary>
        /// 分页查询派工单
        /// </summary>
        [HttpPost]
        public async Task<Response<PagedListDataResp<MesWorkOrder>>> GetPage([FromBody] QueryMesWorkOrderReq request)
        {
            var result = new Response<PagedListDataResp<MesWorkOrder>>();
            try
            {
                result.Data = await _app.GetPageAsync(request);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取全部派工单（自动翻页）
        /// </summary>
        [HttpPost]
        public async Task<Response<PagedListDataResp<MesWorkOrder>>> GetAll([FromBody] QueryMesWorkOrderReq request)
        {
            var result = new Response<PagedListDataResp<MesWorkOrder>>();
            try
            {
                result.Data = await _app.GetAllAsync(request);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 校验派工单
        /// </summary>
        [HttpGet]
        public async Task<Response<bool>> CheckWorkOrder([FromQuery] string workOrder)
        {
            var result = new Response<bool>();
            try
            {
                result.Data = await _app.CheckWorkOrderAsync(workOrder);
            }
            catch (Exception ex)
            {
                result.Code = 500;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 获取派工单详情
        /// </summary>
        [HttpGet]
        public async Task<Response<MesWorkOrderDetail>> GetWorkOrderDetail([FromQuery] string workOrder)
        {
            var result = new Response<MesWorkOrderDetail>();
            try
            {
                result.Data = await _app.GetWorkOrderDetailAsync(workOrder);
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
