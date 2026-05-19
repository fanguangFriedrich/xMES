using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain
{
    public class SyncTaskState
    {
        public string Status { get; set; } = "running"; // running / done / failed
        public int ResultCount { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
