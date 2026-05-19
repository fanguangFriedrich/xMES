using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure;
using OpenAuth.App.Interface;
using OpenAuth.App.Request;
using OpenAuth.App.Response;
using OpenAuth.Repository;
using OpenAuth.Repository.Domain;
using OpenAuth.Repository.Interface;
using SqlSugar;

namespace OpenAuth.App
{
    public class OrgManagerApp : SqlSugarBaseTreeApp<SysOrg>
    {
        private RevelanceManagerApp _revelanceApp;
        /// <summary>
        /// 添加部门
        /// </summary>
        /// <param name="sysOrg">The org.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception">未能找到该组织的父节点信息</exception>
        public string Add(SysOrg sysOrg)
        {
            var loginContext = _auth.GetCurrentUser();
            if (loginContext == null)
            {
                throw new CommonException("登录已过期", Define.INVALID_TOKEN);
            }
            CaculateCascade(sysOrg);

            SugarClient.Ado.BeginTran();
            Repository.Insert(sysOrg);

            //如果当前账号不是SYSTEM，则直接分配
            if (loginContext.User.Account != Define.SYSTEM_USERNAME)
            {
                _revelanceApp.Assign(new AssignReq
                {
                    type = Define.USERORG,
                    firstId = loginContext.User.Id,
                    secIds = new[] { sysOrg.Id }
                });
            }
            SugarClient.Ado.CommitTran();

            return sysOrg.Id;
        }

        public string Update(SysOrg sysOrg)
        {
            if (sysOrg.Id == sysOrg.ParentId)
            {
                throw new Exception("上级节点不能为自己");
            }
            UpdateTreeObj(sysOrg);

            return sysOrg.Id;
        }

        /// <summary>
        /// 删除指定ID的部门及其所有子部门
        /// </summary>
        public void DelOrgCascade(string[] ids)
        {
            var delOrgCascadeIds = SugarClient.Queryable<SysOrg>().Where(u => ids.Contains(u.Id)).Select(u => u.CascadeId).ToArray();
            var delOrgIds = new List<string>();
            foreach (var cascadeId in delOrgCascadeIds)
            {
                delOrgIds.AddRange(SugarClient.Queryable<SysOrg>().Where(u => u.CascadeId.Contains(cascadeId)).Select(u => u.Id).ToArray());
            }

            delOrgIds = delOrgIds.Distinct().ToList();
            var delOrgIdSet = delOrgIds.ToHashSet();
            var allOrgs = SugarClient.Queryable<SysOrg>().Select(u => new SysOrg
            {
                Id = u.Id,
                ParentId = u.ParentId
            }).ToList();
            var orgMap = allOrgs.ToDictionary(u => u.Id, u => u);

            string GetMoveTargetOrgId(string orgId)
            {
                if (!orgMap.TryGetValue(orgId, out var org)) return null;
                var parentId = org.ParentId;
                while (!string.IsNullOrEmpty(parentId) && delOrgIdSet.Contains(parentId))
                {
                    parentId = orgMap.TryGetValue(parentId, out var parentOrg) ? parentOrg.ParentId : null;
                }

                return string.IsNullOrEmpty(parentId) ? null : parentId;
            }

            SugarClient.Ado.BeginTran();
            try
            {
                var userOrgRelations = SugarClient.Queryable<Relevance>()
                    .Where(u => u.RelKey == Define.USERORG && delOrgIds.Contains(u.SecondId))
                    .ToList();

                foreach (var relation in userOrgRelations)
                {
                    var targetOrgId = GetMoveTargetOrgId(relation.SecondId);
                    if (string.IsNullOrEmpty(targetOrgId))
                    {
                        SugarClient.Deleteable<Relevance>().Where(u => u.Id == relation.Id).ExecuteCommand();
                        continue;
                    }

                    var exists = SugarClient.Queryable<Relevance>().Any(u =>
                        u.Id != relation.Id &&
                        u.RelKey == Define.USERORG &&
                        u.FirstId == relation.FirstId &&
                        u.SecondId == targetOrgId);
                    if (exists)
                    {
                        SugarClient.Deleteable<Relevance>().Where(u => u.Id == relation.Id).ExecuteCommand();
                    }
                    else
                    {
                        SugarClient.Updateable<Relevance>()
                            .SetColumns(u => u.SecondId == targetOrgId)
                            .Where(u => u.Id == relation.Id)
                            .ExecuteCommand();
                    }
                }

                SugarClient.Deleteable<SysOrg>().Where(u => delOrgIds.Contains(u.Id)).ExecuteCommand();
                SugarClient.Ado.CommitTran();
            }
            catch
            {
                SugarClient.Ado.RollbackTran();
                throw;
            }

        }

        /// <summary>
        /// 获取所有机构
        /// </summary>
        /// <returns></returns>
        public List<OrgView> LoadAll()
        {
            return SugarClient.Queryable<SysOrg>()
                    .LeftJoin<SysUser>((org, user) => org.ChairmanId ==user.Id)
                    .Select((org,user)=>new OrgView
                    {
                        Id = org.Id.SelectAll(),
                        ChairmanName = user.Name
                    }).ToList();
        }

        public OrgManagerApp(ISqlSugarClient client, IAuth auth,
            RevelanceManagerApp revelanceApp) : base(client, auth)
        {
            _revelanceApp = revelanceApp;
        }

        public string[] GetChairmanId(string[] orgIds)
        {
            return SugarClient.Queryable<SysOrg>().Where(u => orgIds.Contains(u.Id) && u.ChairmanId != null).Select(u => u.ChairmanId).ToArray();
        }

        /// <summary>
        /// 定时任务专用：绕过登录上下文直接插入部门
        /// </summary>
        public string AddOrgFromJob(SysOrg sysOrg)
        {
            // 钉钉同步时部门Id使用deptId，防重必须按Id判断。
            var exists = SugarClient.Queryable<SysOrg>()
                .Any(o => o.Id == sysOrg.Id);
            if (exists) return null;

            try
            {
                sysOrg.CreateTime = DateTime.Now;
                sysOrg.CreateId   = 0;
                CaculateCascade(sysOrg); // 自动填 CascadeId 和 ParentName

                SugarClient.Ado.BeginTran();
                Repository.Insert(sysOrg);
                SugarClient.Ado.CommitTran();

                return sysOrg.Id;
            }
            catch
            {
                SugarClient.Ado.RollbackTran();
                throw;
            }
        }
    }
}
