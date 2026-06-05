using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAuth.Repository.Core;

namespace OpenAuth.Repository.Domain
{
    /// <summary>
    /// 三色灯设备最新状态
    /// </summary>
    [Table("TriColorLampLatestState")]
    public class TriColorLampLatestState : StringEntity
    {
        public TriColorLampLatestState()
        {
            DtuSn = string.Empty;
            DeviceName = string.Empty;
            LampState = 0;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 设备编号
        /// </summary>
        [Description("设备编号")]
        public string DtuSn { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [Description("设备名称")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 当前灯状态
        /// </summary>
        [Description("当前灯状态")]
        public int LampState { get; set; }

        /// <summary>
        /// 当前状态开始时间
        /// </summary>
        [Description("当前状态开始时间")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [Description("最后更新时间")]
        public DateTime UpdateTime { get; set; }
    }
}
