using Infrastructure;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAuth.App.TriColorLamp.Response;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.App.TriColorLamp
{
    public class TriColorLampApp
    {
        private readonly HttpClient _httpClient;
        private readonly TriColorLampOptions _options;
        private readonly ILogger<TriColorLampApp> _logger;

        private string _cachedAccessToken;
        private DateTime _accessTokenExpiry = DateTime.MinValue;

        private const string TokenApiPath = "auth/token";
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
            // 提前5分钟刷新，避免临界点过期
            if (_cachedAccessToken != null && DateTime.UtcNow < _accessTokenExpiry.AddMinutes(-5))
            {
                return _cachedAccessToken;
            }

            (_cachedAccessToken, _accessTokenExpiry) = await GetAccessTokenAsync();

            Console.WriteLine($"[TriColorLamp] AccessToken 已刷新，有效期至: {_accessTokenExpiry:yyyy-MM-dd HH:mm:ss} UTC");

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
            _logger.LogDebug("[TriColorLamp] Token 接口响应状态码: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"获取三色灯 Token 失败 [{response.StatusCode}]: {resultJson}");

            var result = JsonHelper.Deserialize<TriColorLampTokenResponse>(resultJson)
                ?? throw new Exception("解析三色灯 Token 响应失败");

            if (result.Code != 200)
                throw new Exception($"获取三色灯 Token 失败 code={result.Code}: {result.Msg}");

            var token = result.Data == null ? null : result.Data.Token;
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception($"响应中无 data.token 字段: {resultJson}");

            return (token, DateTime.UtcNow.AddSeconds(TokenExpireSeconds));
        }

        public HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
        {
            if (string.IsNullOrWhiteSpace(requestUri))
                throw new ArgumentException("请求地址不能为空", nameof(requestUri));

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

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new Exception("三色灯 BaseUrl 未配置");

            if (string.IsNullOrWhiteSpace(_options.Username))
                throw new Exception("三色灯 Username 未配置");

            if (string.IsNullOrWhiteSpace(_options.Password))
                throw new Exception("三色灯 Password 未配置");
        }

        private string BuildApiUrl(string apiPath)
        {
            ValidateOptions();

            var baseUrl = _options.BaseUrl.EndsWith("/")
                ? _options.BaseUrl
                : $"{_options.BaseUrl}/";

            return new Uri(new Uri(baseUrl), apiPath).ToString();
        }
    }
}
