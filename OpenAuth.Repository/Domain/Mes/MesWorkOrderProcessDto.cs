using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.Mes
{
    // 派工单工序
    public class MesWorkOrderProcessDto
    {
        public int Id { get; set; }
        public string WorkOrder { get; set; } = string.Empty;
        public string WorkOrderCode { get; set; } = string.Empty;
        public string ProcessItemNo { get; set; } = string.Empty;
        public string TextCode { get; set; } = string.Empty;
        public string ProcessText { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string WorkCenter { get; set; } = string.Empty;
        public string WorkCenterName { get; set; } = string.Empty;
        public string CreateBy { get; set; } = string.Empty;
        public double ProductionValue { get; set; }
        public int Status1 { get; set; }
        public long CreatedTime { get; set; }
        public long ModifyedTime { get; set; }
        public long SapCreatedTime { get; set; }
        public bool IsDelete { get; set; }
    }
}
