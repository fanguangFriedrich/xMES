using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using OpenAuth.Repository.Core;
using SqlSugar;

namespace OpenAuth.Repository.Domain
{
    [Table("TriColorLampDailyAvailability")]
    public class TriColorLampDailyAvailability : StringEntity
    {
        public TriColorLampDailyAvailability()
        {
            DataDate = string.Empty;
            DtuSn = string.Empty;
            DeviceName = string.Empty;
            UpdateTime = DateTime.Now;
        }

        [Description("Data date")]
        public string DataDate { get; set; }

        [Description("Device SN")]
        public string DtuSn { get; set; }

        [Description("Device name")]
        [SugarColumn(IsNullable = true)]
        public string DeviceName { get; set; }

        [Description("Work start time")]
        public DateTime WorkStartTime { get; set; }

        [Description("Work end time")]
        public DateTime WorkEndTime { get; set; }

        [Description("Maintenance start time")]
        [SugarColumn(IsNullable = true)]
        public DateTime? MaintenanceStartTime { get; set; }

        [Description("Maintenance end time")]
        [SugarColumn(IsNullable = true)]
        public DateTime? MaintenanceEndTime { get; set; }

        [Description("Work duration seconds")]
        public long WorkDuration { get; set; }

        [Description("Maintenance overlap seconds")]
        public long MaintenanceOverlapDuration { get; set; }

        [Description("Available work seconds")]
        public long AvailableWorkDuration { get; set; }

        [Description("Red duration seconds")]
        public long RedDuration { get; set; }

        [Description("Yellow duration seconds")]
        public long YellowDuration { get; set; }

        [Description("Green duration seconds")]
        public long GreenDuration { get; set; }

        [Description("Blue duration seconds")]
        public long BlueDuration { get; set; }

        [Description("Off duration seconds")]
        public long OffDuration { get; set; }

        [Description("Other duration seconds")]
        public long OtherDuration { get; set; }

        [Description("Availability rate")]
        public decimal AvailabilityRate { get; set; }

        [Description("Update time")]
        public DateTime UpdateTime { get; set; }
    }
}
