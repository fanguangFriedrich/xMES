using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.Mes
{
    // 生产订单工序
    public class MesPoProcess
    {
        public int Id { get; set; }
        public string ProductionOrderNo { get; set; } = string.Empty;
        public int PlanNo { get; set; }
        public int Counter { get; set; }
        public string ProcessItemNo { get; set; } = string.Empty;
        public string TextCode { get; set; } = string.Empty;
        public string ProcessText { get; set; } = string.Empty;
        public string Factory { get; set; } = string.Empty;
        public string WorkCenter { get; set; } = string.Empty;
        public string WorkCenterName { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string ControlCode { get; set; } = string.Empty;
        public double WorkingHour { get; set; }
        public string Unit { get; set; } = string.Empty;
        public long StartDate { get; set; }
        public long EndDate { get; set; }
        public double BasicQuantity { get; set; }
        public double ProcessQuantity { get; set; }
        public string ProcessUnit { get; set; } = string.Empty;
        public double PricingAmount { get; set; }
        public bool IsDelete { get; set; }
        public string ObjectVersion { get; set; } = string.Empty;
        public string Status1 { get; set; } = string.Empty;
        public string Status2 { get; set; } = string.Empty;
        public long CreatedTime { get; set; }
        public long ModifyedTime { get; set; }
    }
}
