using OpenAuth.Repository.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.App.Mes.WorkOrder.Request
{
    public class QueryMesWorkOrderReq
    {
        public int PageNum { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<string> WorkCenters { get; set; } = new();
        public string WorkOrder { get; set; } = string.Empty;
        public string DispatchWorkerJobNo { get; set; } = string.Empty;
        public DateRange StartDateRange { get; set; }
        public DateRange EndDateRange { get; set; }
        public int Status1 { get; set; } = 0;
        public bool IsAdmin { get; set; } = false;
    }
}
