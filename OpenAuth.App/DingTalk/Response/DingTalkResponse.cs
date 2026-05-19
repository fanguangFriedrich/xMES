using OpenAuth.Repository.Domain.DingTalk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAuth.App.DingTalk.Response
{
    /// <summary>
    /// 钉钉 Token 接口响应
    /// </summary>
    public class DingTalkTokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expireIn")]
        public int ExpireIn { get; set; }
    }

    /// <summary>
    /// topapi/v2/user/getuserinfo 外层响应
    /// </summary>
    public class DingTalkCorpUserInfoResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string Errmsg { get; set; }

        [JsonPropertyName("result")]
        public DingTalkCorpUserInfo Result { get; set; }
    }

    public class DingTalkWorkNotificationResponse
    {
        [JsonPropertyName("errcode")]
        public int Errcode { get; set; }

        [JsonPropertyName("errmsg")]
        public string Errmsg { get; set; }

        [JsonPropertyName("task_id")]
        public long TaskId { get; set; }

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; }
    }
}
