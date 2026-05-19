using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.DingTalk
{
    /// <summary>
    /// 钉钉用户信息
    /// </summary>
    public class DingTalkUserInfo
    {
        public string UnionId { get; set; }
        public string Mobile { get; set; }
        public string Nick { get; set; }
        public string StateCode { get; set; }
    }
}
