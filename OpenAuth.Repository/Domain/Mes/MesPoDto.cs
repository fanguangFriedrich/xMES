using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.Mes
{
    // 生产订单
    public class MesPoDto
    {
        public int Id { get; set; }
        public string WorkOrder { get; set; } = string.Empty;
        public string WorkOrderItemNo { get; set; } = string.Empty;
        public string ProductionOrderNo { get; set; } = string.Empty;
        public string ProductionOrderObjectNo { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string ProjectNo { get; set; } = string.Empty;
        public string WbsCode { get; set; } = string.Empty;
        public string WbsId { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string MaterialNo { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string DrawNo { get; set; } = string.Empty;
        public string DrawRevision { get; set; } = string.Empty;
        public string CreateBy { get; set; } = string.Empty;
        public string ProductionOrderCode { get; set; } = string.Empty;
        public bool IsDelete { get; set; }
        public int Status1 { get; set; }
        public long CreatedTime { get; set; }
        public long ModifyedTime { get; set; }
    }
}
