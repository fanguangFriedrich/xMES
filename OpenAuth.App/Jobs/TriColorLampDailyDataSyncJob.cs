using Microsoft.Extensions.Logging;
using OpenAuth.App.TriColorLamp;
using Quartz;
using System;
using System.Threading.Tasks;

namespace OpenAuth.App.Jobs
{
    [DisallowConcurrentExecution]
    public class TriColorLampDailyDataSyncJob : IJob
    {
        private readonly OpenJobApp _openJobApp;
        private readonly TriColorLampApp _triColorLampApp;
        private readonly ILogger<TriColorLampDailyDataSyncJob> _logger;

        public TriColorLampDailyDataSyncJob(
            OpenJobApp openJobApp,
            TriColorLampApp triColorLampApp,
            ILogger<TriColorLampDailyDataSyncJob> logger)
        {
            _openJobApp = openJobApp;
            _triColorLampApp = triColorLampApp;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            _logger.LogInformation("=== 三色灯设备和每日亮灯数据同步开始 {time}，日期 {date} ===", DateTime.Now, date);

            var result = await _triColorLampApp.SyncDeviceDailyLampDataAsync(date);

            _logger.LogInformation("三色灯设备和每日亮灯数据同步完成：设备 {deviceCount} 台，亮灯记录 {lampDataCount} 条，日期 {date}",
                result.DeviceCount,
                result.LampDataCount,
                date);
            _openJobApp.RecordRun(context.JobDetail.Key.Name);
        }
    }
}
