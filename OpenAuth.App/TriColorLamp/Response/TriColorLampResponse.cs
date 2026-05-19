using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace OpenAuth.App.TriColorLamp.Response
{
    public class TriColorLampApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    public class TriColorLampTokenResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }

        [JsonPropertyName("data")]
        public TriColorLampTokenData Data { get; set; }
    }

    public class TriColorLampTokenData
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class TriColorLampStateInfoResponse
    {
        [JsonPropertyName("onlineCount")]
        public int OnlineCount { get; set; }

        [JsonPropertyName("offlineCount")]
        public int OfflineCount { get; set; }

        [JsonPropertyName("alarmCount")]
        public int AlarmCount { get; set; }
    }

    public class TriColorLampRankingResponse
    {
        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("stateCount")]
        public int StateCount { get; set; }
    }

    public class TriColorLampDeviceResponse
    {
        [JsonPropertyName("projectState")]
        public int ProjectState { get; set; }

        [JsonPropertyName("projectType")]
        public string ProjectType { get; set; }

        [JsonPropertyName("dtuId")]
        public long DtuId { get; set; }

        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("deviceId")]
        public long DeviceId { get; set; }
    }
}
