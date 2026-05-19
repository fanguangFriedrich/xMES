using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.DingTalk
{
    public class DingTalkDeptInfo
    {
        [JsonPropertyName("dept_id")]
        public long DeptId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("parent_id")]
        public long ParentId { get; set; }

        [JsonPropertyName("auto_add_user")]
        public bool AutoAddUser { get; set; }

        [JsonPropertyName("create_dept_group")]
        public bool CreateDeptGroup { get; set; }
    }
}
