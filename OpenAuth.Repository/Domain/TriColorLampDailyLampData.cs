using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAuth.Repository.Core;

namespace OpenAuth.Repository.Domain
{
    /// <summary>
    /// 三色灯每日亮灯数据
    /// </summary>
    [Table("TriColorLampDailyLampData")]
    public class TriColorLampDailyLampData : StringEntity
    {
        public TriColorLampDailyLampData()
        {
            DataDate = string.Empty;
            DtuSn = string.Empty;
            DeviceName = string.Empty;
            UpdateTime = DateTime.Now;
        }

        [Description("数据日期")]
        public string DataDate { get; set; }

        [Description("设备编号")]
        public string DtuSn { get; set; }

        [Description("设备名称")]
        public string DeviceName { get; set; }

        [Description("灯状态")]
        public int LampState { get; set; }

        [Description("开始时间")]
        public DateTime? StartTime { get; set; }

        [Description("结束时间")]
        public DateTime? EndTime { get; set; }

        [Description("持续时长")]
        public long Duration { get; set; }

        [Description("最后更新时间")]
        public DateTime UpdateTime { get; set; }
    }
}
