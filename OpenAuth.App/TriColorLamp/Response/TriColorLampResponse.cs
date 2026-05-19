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

    public class TriColorLampDataResponse
    {
        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("lampData")]
        public List<TriColorLampDataItemResponse> LampData { get; set; }
    }

    public class TriColorLampDataItemResponse
    {
        [JsonPropertyName("lampState")]
        public int LampState { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; }

        [JsonPropertyName("duration")]
        public long Duration { get; set; }
    }

    public class TriColorLampCurrentStateResponse
    {
        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("lampState")]
        public int LampState { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; }
    }

    public class TriColorLampRateResponse
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("realRate")]
        public List<TriColorLampRateItemResponse> RealRate { get; set; }
    }

    public class TriColorLampRateItemResponse
    {
        [JsonPropertyName("0")]
        public long State0 { get; set; }

        [JsonPropertyName("1")]
        public long State1 { get; set; }

        [JsonPropertyName("2")]
        public long State2 { get; set; }

        [JsonPropertyName("3")]
        public long State3 { get; set; }

        [JsonPropertyName("4")]
        public long State4 { get; set; }

        [JsonPropertyName("5")]
        public long State5 { get; set; }

        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }
    }

    public class TriColorLampStateCountResponse
    {
        [JsonPropertyName("0")]
        public long State0 { get; set; }

        [JsonPropertyName("1")]
        public long State1 { get; set; }

        [JsonPropertyName("2")]
        public long State2 { get; set; }

        [JsonPropertyName("3")]
        public long State3 { get; set; }

        [JsonPropertyName("4")]
        public long State4 { get; set; }

        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }
    }

    public class TriColorLampCounterLatestResponse
    {
        [JsonPropertyName("FSJ")]
        public string Fsj { get; set; }

        [JsonPropertyName("HSJ")]
        public string Hsj { get; set; }

        [JsonPropertyName("JS")]
        public string Js { get; set; }

        [JsonPropertyName("ZSJ")]
        public string Zsj { get; set; }

        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("modifyTime")]
        public string ModifyTime { get; set; }
    }

    public class TriColorLampCounterHistoryResponse
    {
        [JsonPropertyName("dtuSn")]
        public string DtuSn { get; set; }

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        [JsonPropertyName("counterDatas")]
        public List<TriColorLampCounterHistoryItemResponse> CounterDatas { get; set; }
    }

    public class TriColorLampCounterHistoryItemResponse
    {
        [JsonPropertyName("js")]
        public long Js { get; set; }

        [JsonPropertyName("zsj")]
        public double Zsj { get; set; }

        [JsonPropertyName("fsj")]
        public double Fsj { get; set; }

        [JsonPropertyName("hsj")]
        public double Hsj { get; set; }

        [JsonPropertyName("modifyTime")]
        public string ModifyTime { get; set; }
    }

    public class TriColorLampCounterDayHistoryResponse
    {
        [JsonPropertyName("totalNumber")]
        public long TotalNumber { get; set; }

        [JsonPropertyName("singleDuration")]
        public double SingleDuration { get; set; }

        [JsonPropertyName("averageDuration")]
        public double AverageDuration { get; set; }

        [JsonPropertyName("efficiency")]
        public double Efficiency { get; set; }

        [JsonPropertyName("lampGreenDuration")]
        public long LampGreenDuration { get; set; }

        [JsonPropertyName("startTime")]
        public string StartTime { get; set; }

        [JsonPropertyName("remark")]
        public string Remark { get; set; }
    }

    public class TriColorLampCommandResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }
    }
}
