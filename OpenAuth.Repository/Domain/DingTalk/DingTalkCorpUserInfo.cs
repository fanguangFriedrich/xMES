using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.DingTalk
{
    /// <summary>
    /// 企业模式下的用户信息（对应 OapiV2UserGetuserinfoResponse.UserGetByCodeResponse）
    /// </summary>
    public class DingTalkCorpUserInfo
    {
        [JsonPropertyName("userid")]
        public string UserId { get; set; }

        [JsonPropertyName("device_id")]
        public string DeviceId { get; set; }

        [JsonPropertyName("sys")]
        public bool IsSys { get; set; }

        [JsonPropertyName("sys_level")]
        public int SysLevel { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("unionid")]
        public string UnionId { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }
    }
}
