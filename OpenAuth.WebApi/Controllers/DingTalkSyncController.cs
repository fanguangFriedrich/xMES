using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAuth.App.DingTalk;
using System.Threading.Tasks;

namespace OpenAuth.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [AllowAnonymous]
    public class DingTalkSyncController : ControllerBase
    {
        private readonly DingTalkApp _dingTalkApp;

        public DingTalkSyncController(DingTalkApp dingTalkApp)
        {
            _dingTalkApp = dingTalkApp;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SyncDept()
        {
            var (s, k, f) = await _dingTalkApp.SyncDeptToMesAsync();
            return Ok(new { success = true, message = $"新增{s} 跳过{k} 失败{f}" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SyncUsers()
        {
            var (s, k, f) = await _dingTalkApp.SyncUsersToMesAsync();
            return Ok(new { success = true, message = $"新增{s} 跳过{k} 失败{f}" });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SyncAll()
        {
            var (ds, dk, df, us, uk, uf) = await _dingTalkApp.SyncContactsToMesAsync();
            return Ok(new
            {
                success = true,
                message = $"部门：新增{ds} 跳过{dk} 失败{df}；员工：新增{us} 跳过{uk} 失败{uf}"
            });
        }
    }
}
