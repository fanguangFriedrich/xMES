using Infrastructure;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAuth.App.TriColorLamp.Response;
using System;
using System.Collections.Generic;
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
        private const string StateInfoApiPath = "triColorLamp/stateInfo";
        private const string RankingApiPath = "triColorLamp/ranking";
        private const string UserDtuSnsApiPath = "triColorLamp/userDtuSns";
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

            (_cachedAccessToken, _accessTokenExpiry) = await GetAccessTokenAsync();
            _logger.LogInformation("[TriColorLamp] AccessToken refreshed, expires at {Expiry:u}", _accessTokenExpiry);

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

            return result.Data;
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
    }
}
