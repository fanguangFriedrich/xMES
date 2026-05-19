using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class DingTalkOptions
    {
        public const string SectionName = "DingTalk";
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CorpId { get; set; }
        public string AgentId { get; set; }
    }
}
