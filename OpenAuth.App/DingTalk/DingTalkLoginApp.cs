using OpenAuth.App.Request;
using OpenAuth.App.Response;
using OpenAuth.Repository.Domain.DingTalk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAuth.App.DingTalk
{
    public class DingTalkLoginApp
    {
        private readonly DingTalkApp _dingTalkApp;
        private readonly UserManagerApp _userManagerApp;
        private readonly OrgManagerApp _orgManagerApp;

        public DingTalkLoginApp(DingTalkApp dingTalkApp, UserManagerApp userManagerApp, OrgManagerApp orgManagerApp)
        {
            _dingTalkApp = dingTalkApp;
            _userManagerApp = userManagerApp;
            _orgManagerApp = orgManagerApp;
        }

        /// <summary>
        /// 钉钉登录
        /// <param name="dingTalkUserDetailInfo">钉钉详细用户信息</param>
        /// <param name="enableRegister">允许账户不存在进行注册</param>
        /// </summary>
        public async Task<UserView> LoginOrRegisterByDingTalkAsync(DingTalkUserDetailInfo userDetail, bool enableRegister = false)
        {
            // 1. 获取所有系统部门
            var existingOrgs = _orgManagerApp.LoadAll();

            // 2. 匹配部门：钉钉deptId → 钉钉部门名称 → 系统部门ID
            var orgIdArr = new List<string>();
            if (userDetail.DeptIdList != null && userDetail.DeptIdList.Count > 0)
            {
                foreach (var dingDeptId in userDetail.DeptIdList)
                {
                    // 查钉钉部门详情拿部门名称
                    var dingDept = await _dingTalkApp.GetDeptByIdAsync(dingDeptId);
                    if (dingDept == null) continue;

                    // 用部门名称匹配系统部门
                    var sysOrg = existingOrgs.FirstOrDefault(o => o.Name == dingDept.Name);
                    if (sysOrg != null)
                    {
                        orgIdArr.Add(sysOrg.Id.ToString());
                    }
                }
            }

            // 部门未匹配到则跳过
            if (orgIdArr.Count == 0)
            {
                throw new Exception($"用户 {userDetail.Name}未分配部门或者所属部门未在MES创建");
            }

            // 3. 构建请求，account = JobNumber_Name
            var req = new UpdateUserReq
            {
                Id = userDetail.UserId,
                Account = $"{userDetail.JobNumber}_{userDetail.Name}",
                Name = userDetail.Name ?? string.Empty,
                Password = userDetail.UnionId ?? string.Empty,
                OrganizationIds = string.Join(",", orgIdArr),
                ParentId = string.Empty,
                Status = 0,
                Sex = 0,
            };

            var usr = _userManagerApp.LoginOrRegisterAccountFromDingTalk(req, createId: "49df1602-f5f3-4d52-afb7-3802da619558");

            if(usr == null)
            {
                throw new Exception($"用户 {userDetail.Name}登录失败");
            }

            return usr;
        }
    }
}
