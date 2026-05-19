using OpenAuth.Repository.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.App.SyncTaskManager
{
    public class SyncTaskApp
    {
        private readonly ConcurrentDictionary<string, SyncTaskState> _tasks = new();

        public string CreateTask()
        {
            var taskId = Guid.NewGuid().ToString("N");
            _tasks[taskId] = new SyncTaskState { Status = "running", CreatedAt = DateTime.Now };
            return taskId;
        }

        public void SetSuccess(string taskId, int count)
        {
            if (_tasks.TryGetValue(taskId, out var t))
            {
                t.Status = "done";
                t.ResultCount = count;
            }
        }

        public void SetFailed(string taskId, string error)
        {
            if (_tasks.TryGetValue(taskId, out var t))
            {
                t.Status = "failed";
                t.Error = error;
            }
        }

        public SyncTaskState? Get(string taskId) =>
            _tasks.TryGetValue(taskId, out var t) ? t : null;

        // 清理1小时前的任务，避免内存泄漏
        public void Cleanup() =>
            _tasks.Where(kv => DateTime.Now - kv.Value.CreatedAt > TimeSpan.FromHours(1))
                  .ToList()
                  .ForEach(kv => _tasks.TryRemove(kv.Key, out _));
    }
}
