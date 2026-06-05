using Infrastructure;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAuth.App.TriColorLamp.Response;
using OpenAuth.Repository.Domain;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAuth.App.TriColorLamp
{
    public class TriColorLampApp
    {
        private readonly HttpClient _httpClient;
        private readonly ISqlSugarClient _sugarClient;
        private readonly TriColorLampOptions _options;
        private readonly ILogger<TriColorLampApp> _logger;

        private static readonly SemaphoreSlim TokenRefreshLock = new(1, 1);
        private static readonly SemaphoreSlim LatestStateTableLock = new(1, 1);
        private static readonly SemaphoreSlim DeviceDailyDataTableLock = new(1, 1);
        private static readonly SemaphoreSlim AvailabilityTableLock = new(1, 1);
        private static bool _latestStateTableReady;
        private static bool _deviceDailyDataTableReady;
        private static bool _availabilityTableReady;

        private const int GreenLampState = 3;

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
            ISqlSugarClient sugarClient,
            IOptions<TriColorLampOptions> options,
            ILogger<TriColorLampApp> logger)
        {
            _httpClient = httpClient;
            _sugarClient = sugarClient;
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

        public async Task<List<TriColorLampDeviceResponse>> GetUserDtuSnsFromDbAsync()
        {
            await EnsureDeviceDailyDataTablesAsync();

            var devices = await _sugarClient.Queryable<TriColorLampDevice>()
                .OrderBy(item => item.DtuSn)
                .ToListAsync();

            return devices.Select(ToDeviceResponse).ToList();
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

        public async Task<List<TriColorLampDataResponse>> GetDtuSnListDataFromDbAsync(string dtuSns, string date = null)
        {
            if (string.IsNullOrWhiteSpace(dtuSns))
                throw new ArgumentException("dtuSns cannot be empty", nameof(dtuSns));

            await EnsureDeviceDailyDataTablesAsync();

            var targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date;
            var requestedDtuSns = ParseDtuSns(dtuSns);
            var lampData = await _sugarClient.Queryable<TriColorLampDailyLampData>()
                .Where(item => item.DataDate == targetDate && requestedDtuSns.Contains(item.DtuSn))
                .OrderBy(item => item.DtuSn)
                .OrderBy(item => item.StartTime)
                .ToListAsync();

            if (lampData.Count == 0)
            {
                var apiData = await GetDtuSnListDataAsync(dtuSns, targetDate);
                await SaveDailyLampDataAsync(targetDate, apiData);
                return apiData;
            }

            return MapDailyLampDataResponses(lampData, requestedDtuSns);
        }

        public async Task<TriColorLampDailyAvailabilityResponse> GetDailyAvailabilitySettingAsync(string date, string dtuSn)
        {
            if (string.IsNullOrWhiteSpace(date))
                throw new ArgumentException("date cannot be empty", nameof(date));

            if (string.IsNullOrWhiteSpace(dtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(dtuSn));

            await EnsureAvailabilityTableAsync();

            var setting = await GetOrCreateAvailabilitySettingAsync(date, dtuSn);

            var lampData = await GetDtuSnListDataFromDbAsync(dtuSn, date);
            ApplyAvailabilityCalculation(setting, lampData.FirstOrDefault()?.LampData ?? new List<TriColorLampDataItemResponse>());
            await SaveAvailabilitySettingAsync(setting);

            return ToAvailabilityResponse(setting);
        }

        public async Task<TriColorLampDailyAvailabilityResponse> UpdateDailyAvailabilitySettingAsync(
            TriColorLampDailyAvailabilitySettingRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Date))
                throw new ArgumentException("date cannot be empty", nameof(request.Date));

            if (string.IsNullOrWhiteSpace(request.DtuSn))
                throw new ArgumentException("dtuSn cannot be empty", nameof(request.DtuSn));

            if (string.IsNullOrWhiteSpace(request.WorkStartTime))
                throw new ArgumentException("workStartTime cannot be empty", nameof(request.WorkStartTime));

            if (string.IsNullOrWhiteSpace(request.WorkEndTime))
                throw new ArgumentException("workEndTime cannot be empty", nameof(request.WorkEndTime));

            await EnsureAvailabilityTableAsync();

            var setting = await GetOrCreateAvailabilitySettingAsync(request.Date, request.DtuSn);
            setting.WorkStartTime = ParseDateTimeSetting(request.Date, request.WorkStartTime, nameof(request.WorkStartTime));
            setting.WorkEndTime = ParseDateTimeSetting(request.Date, request.WorkEndTime, nameof(request.WorkEndTime));
            setting.MaintenanceStartTime = ParseNullableDateTimeSetting(request.Date, request.MaintenanceStartTime);
            setting.MaintenanceEndTime = ParseNullableDateTimeSetting(request.Date, request.MaintenanceEndTime);

            if (setting.WorkEndTime <= setting.WorkStartTime)
                throw new ArgumentException("workEndTime must be greater than workStartTime", nameof(request.WorkEndTime));

            if ((setting.MaintenanceStartTime == null && setting.MaintenanceEndTime != null) ||
                (setting.MaintenanceStartTime != null && setting.MaintenanceEndTime == null))
                throw new ArgumentException("maintenanceStartTime and maintenanceEndTime must be both empty or both set");

            if (setting.MaintenanceStartTime != null && setting.MaintenanceEndTime <= setting.MaintenanceStartTime)
                throw new ArgumentException("maintenanceEndTime must be greater than maintenanceStartTime", nameof(request.MaintenanceEndTime));

            var lampData = await GetDtuSnListDataFromDbAsync(request.DtuSn, request.Date);
            ApplyAvailabilityCalculation(setting, lampData.FirstOrDefault()?.LampData ?? new List<TriColorLampDataItemResponse>());
            await SaveAvailabilitySettingAsync(setting);

            return ToAvailabilityResponse(setting);
        }

        public async Task<(int DeviceCount, int LampDataCount)> SyncDeviceDailyLampDataAsync(string date = null)
        {
            await EnsureDeviceDailyDataTablesAsync();

            var devices = await GetUserDtuSnsAsync();
            await SaveCurrentDevicesAsync(devices);

            var dtuSns = string.Join(",", devices.Select(item => item.DtuSn).Where(item => !string.IsNullOrWhiteSpace(item)));
            if (string.IsNullOrWhiteSpace(dtuSns))
                return (devices.Count, 0);

            var targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date;
            var lampData = await GetDtuSnListDataAsync(dtuSns, targetDate);
            var lampDataCount = await SaveDailyLampDataAsync(targetDate, lampData);
            await SaveDailyAvailabilitySettingsAsync(targetDate, devices, lampData);

            return (devices.Count, lampDataCount);
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

        public async Task<List<List<TriColorLampCurrentStateResponse>>> GetDtuSnStateListFromDbAsync(string dtuSns)
        {
            await EnsureLatestStateTableAsync();
            await EnsureAvailabilityTableAsync();

            var query = _sugarClient.Queryable<TriColorLampLatestState>();
            var requestedDtuSns = ParseDtuSns(dtuSns);

            if (requestedDtuSns.Count > 0)
            {
                query = query.Where(item => requestedDtuSns.Contains(item.DtuSn));
            }

            var latestStates = await query.OrderBy(item => item.DtuSn).ToListAsync();
            var targetDate = DateTime.Now.ToString("yyyy-MM-dd");
            var latestDtuSns = latestStates
                .Select(item => item.DtuSn)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList();
            var availabilityRates = latestDtuSns.Count == 0
                ? new Dictionary<string, decimal>()
                : (await _sugarClient.Queryable<TriColorLampDailyAvailability>()
                    .Where(item => item.DataDate == targetDate && latestDtuSns.Contains(item.DtuSn))
                    .ToListAsync())
                    .GroupBy(item => item.DtuSn)
                    .ToDictionary(group => group.Key, group => group.First().AvailabilityRate);

            if (requestedDtuSns.Count > 0)
            {
                var latestStateMap = latestStates.ToDictionary(item => item.DtuSn);
                return requestedDtuSns
                    .Where(dtuSn => latestStateMap.ContainsKey(dtuSn))
                    .Select(dtuSn => new List<TriColorLampCurrentStateResponse>
                    {
                        ToCurrentStateResponse(
                            latestStateMap[dtuSn],
                            availabilityRates.TryGetValue(dtuSn, out var availabilityRate) ? availabilityRate : null)
                    })
                    .ToList();
            }

            return latestStates
                .Select(item => new List<TriColorLampCurrentStateResponse>
                {
                    ToCurrentStateResponse(
                        item,
                        availabilityRates.TryGetValue(item.DtuSn, out var availabilityRate) ? availabilityRate : null)
                })
                .ToList();
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

        public async Task<TriColorLampRealtimeSnapshotResponse> GetRealtimeSnapshotAsync(string date = null)
        {
            var devices = await GetUserDtuSnsAsync();
            var dtuSns = string.Join(",", devices.Select(item => item.DtuSn).Where(item => !string.IsNullOrWhiteSpace(item)));
            var snapshot = new TriColorLampRealtimeSnapshotResponse
            {
                CachedAt = DateTime.Now,
                Devices = devices
            };

            if (string.IsNullOrWhiteSpace(dtuSns))
                return snapshot;

            var targetDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date;
            var stateTask = GetDtuSnStateListAsync(dtuSns);
            //var rateTask = GetDtuSnListRateOfActionAsync(targetDate, dtuSns);
            //var stateCountTask = GetDtuSnListStateCountAsync(dtuSns);
            //var counterTask = GetDtuSnListCounterAsync(dtuSns);

            //await Task.WhenAll(stateTask, rateTask, stateCountTask, counterTask);
            await Task.WhenAll(stateTask);

            snapshot.States = stateTask.Result;
            await SaveLatestStatesAsync(snapshot.States);
            //snapshot.Rates = rateTask.Result;
            //snapshot.StateCounts = stateCountTask.Result;
            //snapshot.Counters = counterTask.Result;
            return snapshot;
        }

        private async Task SaveLatestStatesAsync(List<List<TriColorLampCurrentStateResponse>> states)
        {
            var latestStates = states?
                .Where(group => group != null)
                .SelectMany(group => group)
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.DtuSn))
                .GroupBy(item => item.DtuSn)
                .Select(group => group.First())
                .ToList();

            if (latestStates == null || latestStates.Count == 0)
                return;

            await EnsureLatestStateTableAsync();

            var now = DateTime.Now;
            var dtuSns = latestStates.Select(item => item.DtuSn).ToList();
            var existingStates = await _sugarClient.Queryable<TriColorLampLatestState>()
                .Where(item => dtuSns.Contains(item.DtuSn))
                .ToListAsync();
            var existingMap = existingStates.ToDictionary(item => item.DtuSn);
            var insertList = new List<TriColorLampLatestState>();
            var updateList = new List<TriColorLampLatestState>();

            foreach (var state in latestStates)
            {
                DateTime? startTime = null;
                if (DateTime.TryParse(state.StartTime, out var parsedStartTime))
                {
                    startTime = parsedStartTime;
                }

                if (existingMap.TryGetValue(state.DtuSn, out var entity))
                {
                    entity.DeviceName = state.DeviceName;
                    entity.LampState = state.LampState;
                    entity.StartTime = startTime;
                    entity.UpdateTime = now;
                    updateList.Add(entity);
                }
                else
                {
                    insertList.Add(new TriColorLampLatestState
                    {
                        Id = Guid.NewGuid().ToString(),
                        DtuSn = state.DtuSn,
                        DeviceName = state.DeviceName,
                        LampState = state.LampState,
                        StartTime = startTime,
                        UpdateTime = now
                    });
                }
            }

            if (insertList.Count > 0)
            {
                await _sugarClient.Insertable(insertList).ExecuteCommandAsync();
            }

            if (updateList.Count > 0)
            {
                await _sugarClient.Updateable(updateList)
                    .UpdateColumns(item => new
                    {
                        item.DeviceName,
                        item.LampState,
                        item.StartTime,
                        item.UpdateTime
                    })
                    .ExecuteCommandAsync();
            }
        }

        private async Task EnsureLatestStateTableAsync()
        {
            if (_latestStateTableReady)
                return;

            await LatestStateTableLock.WaitAsync();
            try
            {
                if (_latestStateTableReady)
                    return;

                if (!_sugarClient.DbMaintenance.IsAnyTable("TriColorLampLatestState", false))
                {
                    _sugarClient.CodeFirst.InitTables<TriColorLampLatestState>();
                }
                _latestStateTableReady = true;
            }
            finally
            {
                LatestStateTableLock.Release();
            }
        }

        private async Task EnsureDeviceDailyDataTablesAsync()
        {
            if (_deviceDailyDataTableReady)
                return;

            await DeviceDailyDataTableLock.WaitAsync();
            try
            {
                if (_deviceDailyDataTableReady)
                    return;

                if (!_sugarClient.DbMaintenance.IsAnyTable("TriColorLampDevice", false))
                {
                    _sugarClient.CodeFirst.InitTables<TriColorLampDevice>();
                }

                if (!_sugarClient.DbMaintenance.IsAnyTable("TriColorLampDailyLampData", false))
                {
                    _sugarClient.CodeFirst.InitTables<TriColorLampDailyLampData>();
                }
                _deviceDailyDataTableReady = true;
            }
            finally
            {
                DeviceDailyDataTableLock.Release();
            }
        }

        private async Task EnsureAvailabilityTableAsync()
        {
            if (_availabilityTableReady)
                return;

            await AvailabilityTableLock.WaitAsync();
            try
            {
                if (_availabilityTableReady)
                    return;

                if (!_sugarClient.DbMaintenance.IsAnyTable("TriColorLampDailyAvailability", false))
                {
                    _sugarClient.CodeFirst.InitTables<TriColorLampDailyAvailability>();
                }

                _availabilityTableReady = true;
            }
            finally
            {
                AvailabilityTableLock.Release();
            }
        }

        private async Task SaveCurrentDevicesAsync(List<TriColorLampDeviceResponse> devices)
        {
            if (devices == null || devices.Count == 0)
                return;

            var now = DateTime.Now;
            var dtuSns = devices
                .Select(item => item.DtuSn)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList();
            var existingDevices = await _sugarClient.Queryable<TriColorLampDevice>()
                .Where(item => dtuSns.Contains(item.DtuSn))
                .ToListAsync();
            var existingMap = existingDevices.ToDictionary(item => item.DtuSn);
            var insertList = new List<TriColorLampDevice>();
            var updateList = new List<TriColorLampDevice>();

            foreach (var device in devices.Where(item => !string.IsNullOrWhiteSpace(item.DtuSn)))
            {
                if (existingMap.TryGetValue(device.DtuSn, out var entity))
                {
                    entity.ProjectState = device.ProjectState;
                    entity.ProjectType = device.ProjectType;
                    entity.DtuId = device.DtuId;
                    entity.DeviceName = device.DeviceName;
                    entity.DeviceId = device.DeviceId;
                    entity.UpdateTime = now;
                    updateList.Add(entity);
                }
                else
                {
                    insertList.Add(new TriColorLampDevice
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProjectState = device.ProjectState,
                        ProjectType = device.ProjectType,
                        DtuId = device.DtuId,
                        DtuSn = device.DtuSn,
                        DeviceName = device.DeviceName,
                        DeviceId = device.DeviceId,
                        UpdateTime = now
                    });
                }
            }

            if (insertList.Count > 0)
            {
                await _sugarClient.Insertable(insertList).ExecuteCommandAsync();
            }

            if (updateList.Count > 0)
            {
                await _sugarClient.Updateable(updateList)
                    .UpdateColumns(item => new
                    {
                        item.ProjectState,
                        item.ProjectType,
                        item.DtuId,
                        item.DeviceName,
                        item.DeviceId,
                        item.UpdateTime
                    })
                    .ExecuteCommandAsync();
            }
        }

        private async Task<int> SaveDailyLampDataAsync(string targetDate, List<TriColorLampDataResponse> lampData)
        {
            var dtuSns = lampData?
                .Select(item => item.DtuSn)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList() ?? new List<string>();

            if (dtuSns.Count == 0)
                return 0;

            await _sugarClient.Deleteable<TriColorLampDailyLampData>()
                .Where(item => item.DataDate == targetDate && dtuSns.Contains(item.DtuSn))
                .ExecuteCommandAsync();

            var now = DateTime.Now;
            var insertList = lampData
                .Where(device => device != null && !string.IsNullOrWhiteSpace(device.DtuSn) && device.LampData != null)
                .SelectMany(device => device.LampData.Select(item => new TriColorLampDailyLampData
                {
                    Id = Guid.NewGuid().ToString(),
                    DataDate = targetDate,
                    DtuSn = device.DtuSn,
                    DeviceName = device.DeviceName,
                    LampState = item.LampState,
                    StartTime = TryParseDateTime(item.StartTime),
                    EndTime = TryParseDateTime(item.EndTime),
                    Duration = item.Duration,
                    UpdateTime = now
                }))
                .GroupBy(GetDailyLampDataKey)
                .Select(group => group.First())
                .ToList();

            if (insertList.Count > 0)
            {
                await _sugarClient.Insertable(insertList).ExecuteCommandAsync();
            }

            return insertList.Count;
        }

        private static DateTime? TryParseDateTime(string value)
        {
            if (DateTime.TryParse(value, out var result))
                return result;

            return null;
        }

        private async Task SaveAvailabilitySettingAsync(TriColorLampDailyAvailability setting)
        {
            setting.UpdateTime = DateTime.Now;
            var exists = await _sugarClient.Queryable<TriColorLampDailyAvailability>()
                .AnyAsync(item => item.Id == setting.Id);

            if (exists)
            {
                await _sugarClient.Updateable(setting).ExecuteCommandAsync();
            }
            else
            {
                await _sugarClient.Insertable(setting).ExecuteCommandAsync();
            }
        }

        private async Task SaveDailyAvailabilitySettingsAsync(
            string targetDate,
            List<TriColorLampDeviceResponse> devices,
            List<TriColorLampDataResponse> lampData)
        {
            if (devices == null || devices.Count == 0)
                return;

            await EnsureAvailabilityTableAsync();

            var lampDataMap = (lampData ?? new List<TriColorLampDataResponse>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.DtuSn))
                .GroupBy(item => item.DtuSn)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var device in devices.Where(item => item != null && !string.IsNullOrWhiteSpace(item.DtuSn)))
            {
                var setting = await GetOrCreateAvailabilitySettingAsync(targetDate, device.DtuSn);
                if (string.IsNullOrWhiteSpace(setting.DeviceName))
                {
                    setting.DeviceName = device.DeviceName ?? string.Empty;
                }

                var deviceLampData = lampDataMap.TryGetValue(device.DtuSn, out var matchedData)
                    ? matchedData.LampData ?? new List<TriColorLampDataItemResponse>()
                    : new List<TriColorLampDataItemResponse>();

                ApplyAvailabilityCalculation(setting, deviceLampData);
                await SaveAvailabilitySettingAsync(setting);
            }
        }

        private async Task<TriColorLampDailyAvailability> GetOrCreateAvailabilitySettingAsync(string date, string dtuSn)
        {
            var setting = await _sugarClient.Queryable<TriColorLampDailyAvailability>()
                .FirstAsync(item => item.DataDate == date && item.DtuSn == dtuSn);
            var device = await _sugarClient.Queryable<TriColorLampDevice>()
                .FirstAsync(item => item.DtuSn == dtuSn);

            if (setting == null)
            {
                setting = new TriColorLampDailyAvailability
                {
                    Id = Guid.NewGuid().ToString(),
                    DataDate = date,
                    DtuSn = dtuSn,
                    DeviceName = device?.DeviceName ?? string.Empty,
                    WorkStartTime = DateTime.Parse($"{date} 08:00:00"),
                    WorkEndTime = DateTime.Parse($"{date} 20:00:00"),
                    UpdateTime = DateTime.Now
                };
            }
            else if (string.IsNullOrWhiteSpace(setting.DeviceName) && device != null)
            {
                setting.DeviceName = device.DeviceName;
            }

            return setting;
        }

        private static void ApplyAvailabilityCalculation(
            TriColorLampDailyAvailability setting,
            List<TriColorLampDataItemResponse> lampData)
        {
            var workStart = setting.WorkStartTime;
            var workEnd = setting.WorkEndTime;
            if (workEnd <= workStart)
            {
                workEnd = workStart;
            }

            var maintenanceOverlap = GetOverlapSeconds(
                setting.MaintenanceStartTime,
                setting.MaintenanceEndTime,
                workStart,
                workEnd);
            var workDuration = Math.Max(0, (long)(workEnd - workStart).TotalSeconds);
            var availableWorkDuration = Math.Max(0, workDuration - maintenanceOverlap);

            setting.WorkDuration = workDuration;
            setting.MaintenanceOverlapDuration = maintenanceOverlap;
            setting.AvailableWorkDuration = availableWorkDuration;
            setting.RedDuration = 0;
            setting.YellowDuration = 0;
            setting.GreenDuration = 0;
            setting.BlueDuration = 0;
            setting.OffDuration = 0;
            setting.OtherDuration = 0;

            foreach (var item in lampData.Where(item => item != null)
                         .GroupBy(GetLampDataItemKey)
                         .Select(group => group.First()))
            {
                var itemStart = TryParseDateTime(item.StartTime);
                var itemEnd = TryParseDateTime(item.EndTime);
                if (itemStart == null)
                    continue;

                if (itemEnd == null && item.Duration > 0)
                {
                    itemEnd = itemStart.Value.AddSeconds(item.Duration);
                }

                if (itemEnd == null || itemEnd <= itemStart)
                    continue;

                var duration = GetOverlapSeconds(itemStart, itemEnd, workStart, workEnd)
                    - GetOverlapSeconds(itemStart, itemEnd, setting.MaintenanceStartTime, setting.MaintenanceEndTime);
                duration = Math.Max(0, duration);

                switch (item.LampState)
                {
                    case 0:
                        setting.OffDuration += duration;
                        break;
                    case 1:
                        setting.RedDuration += duration;
                        break;
                    case 2:
                        setting.YellowDuration += duration;
                        break;
                    case GreenLampState:
                        setting.GreenDuration += duration;
                        break;
                    case 4:
                        setting.BlueDuration += duration;
                        break;
                    default:
                        setting.OtherDuration += duration;
                        break;
                }
            }

            setting.AvailabilityRate = availableWorkDuration > 0
                ? Math.Round((decimal)setting.GreenDuration / availableWorkDuration * 100, 2)
                : 0;
        }

        private static long GetOverlapSeconds(DateTime? firstStart, DateTime? firstEnd, DateTime? secondStart, DateTime? secondEnd)
        {
            if (firstStart == null || firstEnd == null || secondStart == null || secondEnd == null)
                return 0;

            return GetOverlapSeconds(firstStart.Value, firstEnd.Value, secondStart.Value, secondEnd.Value);
        }

        private static long GetOverlapSeconds(DateTime firstStart, DateTime firstEnd, DateTime secondStart, DateTime secondEnd)
        {
            var start = firstStart > secondStart ? firstStart : secondStart;
            var end = firstEnd < secondEnd ? firstEnd : secondEnd;

            if (end <= start)
                return 0;

            return (long)(end - start).TotalSeconds;
        }

        private static DateTime ParseDateTimeSetting(string date, string value, string parameterName)
        {
            var normalizedValue = NormalizeDateTimeValue(date, value);
            if (DateTime.TryParse(normalizedValue, out var result))
                return result;

            throw new ArgumentException($"{parameterName} is invalid", parameterName);
        }

        private static DateTime? ParseNullableDateTimeSetting(string date, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return ParseDateTimeSetting(date, value, nameof(value));
        }

        private static string NormalizeDateTimeValue(string date, string value)
        {
            var trimmedValue = value.Trim();
            if (trimmedValue.Length <= 8 && trimmedValue.Count(item => item == ':') >= 1)
            {
                return $"{date} {trimmedValue}";
            }

            return trimmedValue;
        }

        private static List<string> ParseDtuSns(string dtuSns)
        {
            if (string.IsNullOrWhiteSpace(dtuSns))
                return new List<string>();

            return dtuSns.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList();
        }

        private static TriColorLampCurrentStateResponse ToCurrentStateResponse(
            TriColorLampLatestState state,
            decimal? availabilityRate = null)
        {
            return new TriColorLampCurrentStateResponse
            {
                DtuSn = state.DtuSn,
                DeviceName = state.DeviceName,
                LampState = state.LampState,
                StartTime = state.StartTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                AvailabilityRate = availabilityRate
            };
        }

        private static TriColorLampDeviceResponse ToDeviceResponse(TriColorLampDevice device)
        {
            return new TriColorLampDeviceResponse
            {
                ProjectState = device.ProjectState,
                ProjectType = device.ProjectType,
                DtuId = device.DtuId,
                DtuSn = device.DtuSn,
                DeviceName = device.DeviceName,
                DeviceId = device.DeviceId
            };
        }

        private static List<TriColorLampDataResponse> MapDailyLampDataResponses(
            List<TriColorLampDailyLampData> lampData,
            List<string> requestedDtuSns)
        {
            var dataMap = lampData
                .GroupBy(item => item.DtuSn)
                .ToDictionary(
                    group => group.Key,
                    group => new TriColorLampDataResponse
                    {
                        DtuSn = group.Key,
                        DeviceName = group.FirstOrDefault()?.DeviceName,
                        LampData = group
                            .GroupBy(GetDailyLampDataKey)
                            .Select(itemGroup => itemGroup.First())
                            .OrderBy(item => item.StartTime)
                            .Select(ToLampDataItemResponse)
                            .ToList()
                    });

            return requestedDtuSns
                .Where(dtuSn => dataMap.ContainsKey(dtuSn))
                .Select(dtuSn => dataMap[dtuSn])
                .ToList();
        }

        private static TriColorLampDataItemResponse ToLampDataItemResponse(TriColorLampDailyLampData item)
        {
            return new TriColorLampDataItemResponse
            {
                LampState = item.LampState,
                StartTime = item.StartTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = item.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                Duration = item.Duration
            };
        }

        private static string GetDailyLampDataKey(TriColorLampDailyLampData item)
        {
            return $"{item.DataDate}|{item.DtuSn}|{item.LampState}|{FormatDateTime(item.StartTime)}|{FormatDateTime(item.EndTime)}";
        }

        private static string GetLampDataItemKey(TriColorLampDataItemResponse item)
        {
            return $"{item.LampState}|{item.StartTime}|{item.EndTime}";
        }

        private static TriColorLampDailyAvailabilityResponse ToAvailabilityResponse(TriColorLampDailyAvailability setting)
        {
            return new TriColorLampDailyAvailabilityResponse
            {
                DataDate = setting.DataDate,
                DtuSn = setting.DtuSn,
                DeviceName = setting.DeviceName,
                WorkStartTime = FormatDateTime(setting.WorkStartTime),
                WorkEndTime = FormatDateTime(setting.WorkEndTime),
                MaintenanceStartTime = FormatDateTime(setting.MaintenanceStartTime),
                MaintenanceEndTime = FormatDateTime(setting.MaintenanceEndTime),
                WorkDuration = setting.WorkDuration,
                MaintenanceOverlapDuration = setting.MaintenanceOverlapDuration,
                AvailableWorkDuration = setting.AvailableWorkDuration,
                RedDuration = setting.RedDuration,
                YellowDuration = setting.YellowDuration,
                GreenDuration = setting.GreenDuration,
                BlueDuration = setting.BlueDuration,
                OffDuration = setting.OffDuration,
                OtherDuration = setting.OtherDuration,
                AvailabilityRate = setting.AvailabilityRate,
                UpdateTime = FormatDateTime(setting.UpdateTime)
            };
        }

        private static string FormatDateTime(DateTime? value)
        {
            return value?.ToString("yyyy-MM-dd HH:mm:ss");
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
