using OpenAuth.App.Mes.WorkOrder.Request;
using OpenAuth.App.Response;
using OpenAuth.Repository.Domain.Mes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenAuth.App.Mes.WorkOrder
{
    public class MesWorkOrderApp
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.patjoin.com.cn:9090/Oas/Other/Mes/Common/GetMesWorkOrderDtoPage";
        private const string DetailApiUrl = "https://api.patjoin.com.cn:9090/Oas/Other/Mes/Common/GetMesWorkDetailsDto";

        public MesWorkOrderApp(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 分页查询派工单
        /// </summary>
        public async Task<PagedListDataResp<MesWorkOrder>> GetPageAsync(QueryMesWorkOrderReq request)
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(ApiUrl, content);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();

            // 先解析外层 API 响应
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<MesWorkOrderPageResult>>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse == null || !apiResponse.Success)
                throw new Exception($"接口请求失败: {apiResponse?.Errmsg}");

            // 转换为系统统一的 PagedListDataResp
            return new PagedListDataResp<MesWorkOrder>
            {
                Count = apiResponse.Result.TotalCount,
                Data = apiResponse.Result.PageList
            };
        }

        /// <summary>
        /// 获取全部派工单（自动翻页）
        /// </summary>
        public async Task<PagedListDataResp<MesWorkOrder>> GetAllAsync(QueryMesWorkOrderReq request)
        {
            var allList = new List<MesWorkOrder>();
            request.PageNum = 1;
            int pageCount;

            do
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(ApiUrl, content);
                response.EnsureSuccessStatusCode();

                var resultJson = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<MesWorkOrderPageResult>>(resultJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse == null || !apiResponse.Success)
                    throw new Exception($"接口请求失败: {apiResponse?.Errmsg}");

                allList.AddRange(apiResponse.Result.PageList);
                pageCount = apiResponse.Result.PageCount;
                request.PageNum++;

            } while (request.PageNum <= pageCount);

            return new PagedListDataResp<MesWorkOrder>
            {
                Count = allList.Count,
                Data = allList
            };
        }

        /// <summary>
        /// 校验派工单
        /// </summary>
        public async Task<bool> CheckWorkOrderAsync(string workOrder)
        {
            var url = $"https://api.patjoin.com.cn:9090/Oas/Other/Mes/Common/CheckMesWorkOrderDto?workOrder={Uri.EscapeDataString(workOrder)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resultJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (apiResponse == null)
                throw new Exception("接口响应解析失败");
            if (!apiResponse.Success)
                throw new Exception($"校验派工单失败: {apiResponse.Errmsg}");
            return apiResponse.Result;
        }

        /// <summary>
        /// 获取派工单详情
        /// </summary>
        public async Task<MesWorkOrderDetail> GetWorkOrderDetailAsync(string workOrder)
        {
            var url = $"{DetailApiUrl}?workOrder={Uri.EscapeDataString(workOrder)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resultJson = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<MesWorkOrderDetail>>(resultJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (apiResponse == null || !apiResponse.Success)
                throw new Exception($"获取派工单详情失败: {apiResponse?.Errmsg}");
            return apiResponse.Result;
        }
    }

    // 仅用于解析该第三方接口的结构，内部使用
    internal class MesWorkOrderPageResult
    {
        public int PageNum { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int TotalCount { get; set; }
        public List<MesWorkOrder> PageList { get; set; } = new();
    }

    internal class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Errmsg { get; set; }
        public int Errcode { get; set; }
        public T Result { get; set; }
    }
}
