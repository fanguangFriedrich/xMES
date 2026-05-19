using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.Repository.Domain.Mes
{
    // 派工单详情聚合类
    public class MesWorkOrderDetail
    {
        public MesWorkOrderDto MesWorkOrderDto { get; set; } = new();
        public List<MesPoDto> MesPoDtos { get; set; } = new();
        public List<MesWorkOrderProcessDto> MesWorkOrderProcessDtos { get; set; } = new();
        public List<MesPoProcess> MesPoProcesses { get; set; } = new();
    }
}
