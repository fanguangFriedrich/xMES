using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAuth.App.DingTalk.Request
{
    public class DingTalkReq
    {
    }

    public class DingTalkWorkNotificationReq
    {
        [JsonProperty("userid_list")]
        [JsonPropertyName("userid_list")]
        public string UseridList { get; set; }

        [JsonProperty("dept_id_list")]
        [JsonPropertyName("dept_id_list")]
        public string DeptIdList { get; set; }

        [JsonProperty("to_all_user")]
        [JsonPropertyName("to_all_user")]
        public bool? ToAllUser { get; set; }

        [JsonProperty("agent_id")]
        [JsonPropertyName("agent_id")]
        public long? AgentId { get; set; }

        [JsonProperty("msg")]
        [JsonPropertyName("msg")]
        public JsonElement Msg { get; set; }
    }

    public class DingTalkTextWorkNotificationReq
    {
        [JsonProperty("userid_list")]
        [JsonPropertyName("userid_list")]
        public string UseridList { get; set; }

        [JsonProperty("dept_id_list")]
        [JsonPropertyName("dept_id_list")]
        public string DeptIdList { get; set; }

        [JsonProperty("to_all_user")]
        [JsonPropertyName("to_all_user")]
        public bool? ToAllUser { get; set; }

        [JsonProperty("agent_id")]
        [JsonPropertyName("agent_id")]
        public long? AgentId { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
