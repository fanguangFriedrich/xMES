using OpenAuth.Repository.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.Mes
{
    public class MesWorkOrder
    {
        public MesWorkOrder() { }

        public int Id { get; set; }
        public string WorkOrder { get; set; } = string.Empty;
        public string WorkOrderSerialNo { get; set; } = string.Empty;
        public string DispatchWorkers { get; set; } = string.Empty;
        public long StartDate { get; set; }
        public long EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Status1 { get; set; }
        public long CreatedTime { get; set; }
        public long ModifyedTime { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public string ModifyBy { get; set; } = string.Empty;
        public bool IsDelete { get; set; }
    }
}
