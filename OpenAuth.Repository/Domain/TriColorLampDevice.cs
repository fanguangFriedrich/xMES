using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAuth.Repository.Core;

namespace OpenAuth.Repository.Domain
{
    /// <summary>
    /// 三色灯当前设备信息
    /// </summary>
    [Table("TriColorLampDevice")]
    public class TriColorLampDevice : StringEntity
    {
        public TriColorLampDevice()
        {
            ProjectType = string.Empty;
            DtuSn = string.Empty;
            DeviceName = string.Empty;
            UpdateTime = DateTime.Now;
        }

        [Description("项目状态")]
        public int ProjectState { get; set; }

        [Description("项目类型")]
        public string ProjectType { get; set; }

        [Description("DTU ID")]
        public long DtuId { get; set; }

        [Description("设备编号")]
        public string DtuSn { get; set; }

        [Description("设备名称")]
        public string DeviceName { get; set; }

        [Description("设备ID")]
        public long DeviceId { get; set; }

        [Description("最后更新时间")]
        public DateTime UpdateTime { get; set; }
    }
}
