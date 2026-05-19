using Microsoft.Extensions.Logging;
using OpenAuth.App.DingTalk;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.App.Jobs
{
    [DisallowConcurrentExecution]
    public class DingTalkSyncJob : IJob
    {
        private readonly OpenJobApp _openJobApp;
        private readonly DingTalkApp _dingTalkApp;
        private readonly ILogger<DingTalkSyncJob> _logger;

        public DingTalkSyncJob(
            OpenJobApp openJobApp,
            DingTalkApp dingTalkApp,
            ILogger<DingTalkSyncJob> logger)
        {
            _openJobApp  = openJobApp;
            _dingTalkApp = dingTalkApp;
            _logger      = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("=== 钉钉通讯录定时同步开始 {time} ===", DateTime.Now);

            var dingDepts = await _dingTalkApp.GetAllDeptListWithRootAsync();

            // 1. 先同步部门
            var (ds, dk, df) = await _dingTalkApp.SyncDeptToMesAsync(dingDepts);
            _logger.LogInformation("部门同步：新增{s} 跳过{k} 失败{f}", ds, dk, df);

            // 2. 再同步员工（部门必须先同步，员工才能匹配到部门）
            var (us, uk, uf) = await _dingTalkApp.SyncUsersToMesAsync(dingDepts);
            _logger.LogInformation("员工同步：新增{s} 跳过{k} 失败{f}", us, uk, uf);

            var jobId = context.JobDetail.Key.Name;
            _openJobApp.RecordRun(jobId);
        }
    }
}
