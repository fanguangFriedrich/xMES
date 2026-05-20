using Microsoft.Extensions.Logging;
using OpenAuth.App.TriColorLamp;
using Quartz;
using System;
using System.Threading.Tasks;

namespace OpenAuth.App.Jobs
{
    [DisallowConcurrentExecution]
    public class TriColorLampSyncJob : IJob
    {
        private readonly OpenJobApp _openJobApp;
        private readonly TriColorLampApp _triColorLampApp;
        private readonly ILogger<TriColorLampSyncJob> _logger;

        public TriColorLampSyncJob(
            OpenJobApp openJobApp,
            TriColorLampApp triColorLampApp,
            ILogger<TriColorLampSyncJob> logger)
        {
            _openJobApp = openJobApp;
            _triColorLampApp = triColorLampApp;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("=== 三色灯状态和数据定时获取开始 {time} ===", DateTime.Now);

            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var snapshot = await _triColorLampApp.GetRealtimeSnapshotAsync(date);
            TriColorLampRealtimeCache.Set(snapshot);

            _logger.LogInformation("三色灯定时获取完成：设备 {count} 台，日期 {date}", snapshot.Devices.Count, date);
            _openJobApp.RecordRun(context.JobDetail.Key.Name);
        }
    }
}
