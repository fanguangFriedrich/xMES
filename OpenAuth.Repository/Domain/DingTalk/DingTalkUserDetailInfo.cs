using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.DingTalk
{
    public class DingTalkUserDetailInfo
    {
        [JsonPropertyName("userid")]
        public string? UserId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("org_email")]
        public string? OrgEmail { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("job_number")]
        public string? JobNumber { get; set; }

        [JsonPropertyName("work_place")]
        public string? WorkPlace { get; set; }

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("unionid")]
        public string? UnionId { get; set; }

        [JsonPropertyName("remark")]
        public string? Remark { get; set; }

        [JsonPropertyName("telephone")]
        public string? Telephone { get; set; }

        [JsonPropertyName("state_code")]
        public string? StateCode { get; set; }

        [JsonPropertyName("extension")]
        public string? Extension { get; set; }

        [JsonPropertyName("hired_date")]
        public long HiredDate { get; set; }

        [JsonPropertyName("leader")]
        public bool Leader { get; set; }

        [JsonPropertyName("boss")]
        public bool Boss { get; set; }

        [JsonPropertyName("admin")]
        public bool Admin { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("exclusive_account")]
        public bool ExclusiveAccount { get; set; }

        [JsonPropertyName("hide_mobile")]
        public bool HideMobile { get; set; }

        [JsonPropertyName("dept_id_list")]
        public List<long>? DeptIdList { get; set; }

        [JsonPropertyName("dept_order")]
        public long DeptOrder { get; set; }
    }
}
