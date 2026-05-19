<template>
  <div class="dingtalkmanager-container">
    <div class="page-header">
      <div class="page-title">
        <i class="el-icon-office-building title-icon"></i>
        <span>{{ currentModuleTitle }}</span>
      </div>
      <el-button
        v-if="activeModule !== 'home'"
        size="small"
        icon="el-icon-back"
        @click="backToHome"
      >返回主页</el-button>
    </div>

    <div v-if="activeModule === 'home'" class="module-grid">
      <div
        v-for="module in featureModules"
        :key="module.key"
        class="module-card"
        @click="openModule(module.key)"
      >
        <div class="module-icon" :class="module.iconClass">
          <i :class="module.icon"></i>
        </div>
        <div class="module-content">
          <div class="module-title">{{ module.title }}</div>
          <div class="module-desc">{{ module.desc }}</div>
        </div>
        <i class="el-icon-arrow-right module-arrow"></i>
      </div>
    </div>

    <template v-if="activeModule === 'sync'">
    <!-- 部门管理 -->
    <div class="section-card">
      <div class="section-header">
        <span class="section-title">
          <i class="el-icon-folder"></i> 部门管理
        </span>
        <div class="section-actions">
          <el-button
            type="primary"
            size="small"
            icon="el-icon-search"
            :loading="loadingDepts"
            @click="fetchAllDeptList"
          >查询钉钉公司组织架构</el-button>
          <el-button
            type="success"
            size="small"
            icon="el-icon-refresh"
            :disabled="selectedDeptRows.length === 0"
            :loading="syncing"
            @click="syncSelectedDepts"
          >同步钉钉公司组织架构（{{ selectedDeptRows.length }}）</el-button>
        </div>
      </div>

      <!-- 搜索过滤 -->
      <div v-if="deptList.length > 0" class="search-bar">
        <el-input
          v-model="deptSearchKeyword"
          placeholder="按部门名称/部门ID/父部门ID搜索"
          size="small"
          prefix-icon="el-icon-search"
          clearable
          style="width: 260px;"
        />
        <el-button
          size="small"
          :type="isAllDeptSelected ? 'warning' : 'default'"
          :icon="isAllDeptSelected ? 'el-icon-minus' : 'el-icon-check'"
          style="margin-left: 10px;"
          @click="toggleSelectAllDepts"
        >{{ isAllDeptSelected ? `取消全选（${selectedDeptRows.length}）` : `全选全部部门（${filteredDeptList.length}）` }}</el-button>
      </div>
      <el-table
        v-loading="loadingDepts"
        :data="pagedDeptList"
        border
        stripe
        size="small"
        style="width: 100%; margin-top: 12px;"
        @selection-change="handleDeptSelectionChange"
      >
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="deptId" label="部门ID" width="120" align="center" />
        <el-table-column prop="name" label="部门名称" min-width="160" />
        <el-table-column prop="parentId" label="父部门ID" width="120" align="center">
          <template #default="{ row }">
            <el-tag size="mini" type="info">{{ row.parentId === 1 ? '根部门' : row.parentId }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="autoAddUser" label="自动添加成员" width="130" align="center">
          <template #default="{ row }">
            <el-tag :type="row.autoAddUser ? 'success' : 'info'" size="mini">{{ row.autoAddUser ? '是' : '否' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createDeptGroup" label="创建部门群" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="row.createDeptGroup ? 'success' : 'info'" size="mini">{{ row.createDeptGroup ? '是' : '否' }}</el-tag>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="filteredDeptList.length > 0" class="pagination-bar">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next, jumper"
          :total="filteredDeptList.length"
          :page-sizes="[10, 20, 50, 100]"
          :page-size="deptPageSize"
          :current-page="deptCurrentPage"
          @size-change="handleDeptSizeChange"
          @current-change="handleDeptCurrentChange"
        />
      </div>
      <div v-if="deptList.length > 0" class="table-footer">
        共 <strong>{{ deptList.length }}</strong> 个部门，已选 <strong>{{ selectedDeptRows.length }}</strong> 个
      </div>
      <el-empty v-if="!loadingDepts && deptList.length === 0" description="暂无数据，请点击「查询钉钉公司组织架构」" />
    </div>

    <!-- 员工管理 -->
    <div class="section-card">
      <div class="section-header">
        <span class="section-title">
          <i class="el-icon-user"></i> 员工管理
        </span>
        <div class="section-actions">
          <el-button
            type="primary"
            size="small"
            icon="el-icon-search"
            :loading="loadingUsers"
            @click="fetchAllUsers"
          >查询钉钉公司所有员工</el-button>
          <el-button
            type="success"
            size="small"
            icon="el-icon-refresh"
            :disabled="selectedUserRows.length === 0"
            :loading="syncingUsers"
            @click="syncSelectedUsers"
          >同步钉钉员工（{{ selectedUserRows.length }}）</el-button>
        </div>
      </div>

      <!-- 搜索过滤 -->
      <div v-if="userList.length > 0" class="search-bar">
        <el-input
          v-model="userSearchKeyword"
          placeholder="按姓名/工号/手机号搜索"
          size="small"
          prefix-icon="el-icon-search"
          clearable
          style="width: 260px;"
        />
        <el-button
          size="small"
          :type="isAllUserSelected ? 'warning' : 'default'"
          :icon="isAllUserSelected ? 'el-icon-minus' : 'el-icon-check'"
          style="margin-left: 10px;"
          @click="toggleSelectAllUsers"
        >{{ isAllUserSelected ? `取消全选（${selectedUserRows.length}）` : `全选全部员工（${filteredUserList.length}）` }}</el-button>
      </div>
      <div v-if="fetchProgress" class="fetch-progress">
        <i class="el-icon-loading"></i> {{ fetchProgress }}
      </div>
      <el-table
        v-loading="loadingUsers"
        :data="pagedUserList"
        border
        stripe
        size="small"
        style="width: 100%; margin-top: 12px;"
        @selection-change="handleUserSelectionChange"
      >
        <el-table-column type="selection" width="50" align="center" />
        <el-table-column prop="userId" label="钉钉UserId" width="180" show-overflow-tooltip />
        <el-table-column label="头像" width="70" align="center">
          <template #default="{ row }">
            <el-avatar v-if="row.avatar" :src="row.avatar" :size="30" />
            <el-avatar v-else icon="el-icon-user" :size="30" />
          </template>
        </el-table-column>
        <el-table-column prop="name" label="姓名" width="100" align="center" />
        <el-table-column prop="jobNumber" label="工号" width="100" align="center" />
        <el-table-column prop="title" label="职位" width="130" show-overflow-tooltip />
        <el-table-column prop="mobile" label="手机号" width="130" align="center" />
        <el-table-column prop="email" label="邮箱" min-width="160" show-overflow-tooltip />
        <el-table-column prop="workPlace" label="工作地点" width="130" show-overflow-tooltip />
        <el-table-column label="入职时间" width="120" align="center">
          <template #default="{ row }">
            {{ formatHiredDate(row.hiredDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="active" label="在职状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="row.active ? 'success' : 'danger'" size="mini">{{ row.active ? '在职' : '离职' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="leader" label="是否主管" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.leader" type="warning" size="mini">主管</el-tag>
            <span v-else>-</span>
          </template>
        </el-table-column>
        
      </el-table>
      <div v-if="filteredUserList.length > 0" class="pagination-bar">
      <el-pagination
        background
        layout="total, sizes, prev, pager, next, jumper"
        :total="filteredUserList.length"
        :page-sizes="[10, 20, 50, 100]"
        :page-size="userPageSize"
        :current-page="userCurrentPage"
        @size-change="handleUserSizeChange"
        @current-change="handleUserCurrentChange"
      />
    </div>
      <div v-if="userList.length > 0" class="table-footer">
        共 <strong>{{ userList.length }}</strong> 名员工，已选 <strong>{{ selectedUserRows.length }}</strong> 名
      </div>
      <el-empty v-if="!loadingUsers && userList.length === 0" description="暂无数据，请点击「查询钉钉公司所有员工」" />
    </div>
    </template>

    <template v-else-if="activeModule === 'notify'">
    <!-- 发送钉钉工作通知 -->
    <div class="section-card">
      <div class="section-header">
        <span class="section-title">
          <i class="el-icon-message"></i> 发送钉钉工作通知
        </span>
      </div>

      <el-form class="notify-form" label-width="90px" size="small">
        <el-form-item label="接收用户">
          <div class="notify-user-row">
            <el-button type="primary" icon="el-icon-user" size="small" @click="openNotifyUserDialog">
              选择MES用户
            </el-button>
            <el-button
              v-if="notificationUserIds.length > 0"
              type="text"
              size="small"
              @click="clearNotifyUsers"
            >清空</el-button>
            <span class="notify-user-count">已选择 {{ notificationUserIds.length }} 人</span>
          </div>
          <div v-if="notificationUserNames" class="notify-selected-users">
            {{ notificationUserNames }}
          </div>
        </el-form-item>

        <el-form-item label="通知内容">
          <el-input
            v-model="notificationContent"
            type="textarea"
            :rows="5"
            maxlength="2000"
            show-word-limit
            placeholder="请输入要发送到钉钉的文本工作通知内容"
          />
        </el-form-item>

        <el-form-item>
          <el-button
            type="success"
            icon="el-icon-s-promotion"
            :loading="sendingDingTalkNotification"
            :disabled="notificationUserIds.length === 0 || !notificationContent.trim()"
            @click="sendDingTalkTextNotification"
          >发送工作通知</el-button>
        </el-form-item>
      </el-form>

      <el-dialog
        class="dialog-mini user-dialog"
        title="选择通知接收用户"
        :visible.sync="dialogNotifyUsers"
        width="720px"
      >
        <selectUsersCom
          ref="notifyUserSelector"
          v-if="dialogNotifyUsers"
          :hiddenFooter="true"
          :loginKey="'loginUser'"
          :ignore-auth="true"
          :users.sync="notificationUserIds"
          :userNames.sync="notificationUserNames"
        />
        <div slot="footer" style="text-align: right;">
          <el-button size="small" @click="dialogNotifyUsers = false">取消</el-button>
          <el-button size="small" type="primary" @click="handleConfirmNotifyUsers">确定</el-button>
        </div>
      </el-dialog>
    </div>
    </template>
  </div>
</template>

<script>
import * as orgs from '@/api/orgs'
import * as login from '@/api/login'
import * as users from '@/api/users'
import { getAllDeptList, getDeptById, getDeptUserList, sendTextWorkNotification } from '@/api/dingtalk'
import { listToTreeSelect } from '@/utils'
import selectUsersCom from '@/components/SelectUsersCom'

export default {
  name: 'dingtalkmanager',
  components: {
    selectUsersCom
  },
  data() {
    return {
      activeModule: 'home',
      featureModules: [
        {
          key: 'sync',
          title: '同步钉钉部门和成员',
          desc: '查询钉钉组织架构与员工，并同步到MES系统。',
          icon: 'el-icon-refresh',
          iconClass: 'sync'
        },
        {
          key: 'notify',
          title: '发送钉钉工作通知',
          desc: '选择MES系统用户，发送文本钉钉工作通知。',
          icon: 'el-icon-message',
          iconClass: 'notify'
        }
      ],

      // 部门
      deptList: [],
      selectedDeptRows: [],
      loadingDepts: false,
      syncing: false,
      deptSearchKeyword: '',
      deptCurrentPage: 1,
      deptPageSize: 20,
      isAllDeptSelected: false,

      // 员工
      userList: [],
      selectedUserRows: [],
      loadingUsers: false,
      userSearchKeyword: '',
      userCurrentPage: 1,  // 新增
      userPageSize: 20,    // 新增
      syncingUsers: false,
      isAllUserSelected: false, // 新增
      fetchProgress: '',

      // 钉钉工作通知
      dialogNotifyUsers: false,
      notificationUserIds: [],
      notificationUserNames: '',
      notificationContent: '',
      sendingDingTalkNotification: false,
    }
  },
  computed: {
    currentModuleTitle() {
      if (this.activeModule === 'home') return '钉钉管理'
      const module = this.featureModules.find(item => item.key === this.activeModule)
      return module ? module.title : '钉钉管理'
    },

    // 部门搜索过滤
    filteredDeptList() {
      const kw = this.deptSearchKeyword.trim().toLowerCase()
      if (!kw) return this.deptList
      return this.deptList.filter(d =>
        (d.name && d.name.toLowerCase().includes(kw)) ||
        (d.deptId !== undefined && String(d.deptId).includes(kw)) ||
        (d.parentId !== undefined && String(d.parentId).includes(kw))
      )
    },
    // 部门分页
    pagedDeptList() {
      const start = (this.deptCurrentPage - 1) * this.deptPageSize
      return this.filteredDeptList.slice(start, start + this.deptPageSize)
    },
    // 员工搜索过滤
    filteredUserList() {
      const kw = this.userSearchKeyword.trim().toLowerCase()
      if (!kw) return this.userList
      return this.userList.filter(u =>
        (u.name && u.name.toLowerCase().includes(kw)) ||
        (u.jobNumber && u.jobNumber.toLowerCase().includes(kw)) ||
        (u.mobile && u.mobile.includes(kw))
      )
    },
    // 新增：在过滤结果基础上分页
    pagedUserList() {
      const start = (this.userCurrentPage - 1) * this.userPageSize
      return this.filteredUserList.slice(start, start + this.userPageSize)
    }
  },
  // 搜索关键词变化时重置到第一页，在 watch 中添加
  watch: {
    deptSearchKeyword() {
      this.deptCurrentPage = 1
      this.selectedDeptRows = []
      this.isAllDeptSelected = false
    },
    userSearchKeyword() {
      this.userCurrentPage = 1
      this.selectedUserRows = []       // 新增
      this.isAllUserSelected = false   // 新增
    }
  },
  methods: {
    openModule(moduleKey) {
      this.activeModule = moduleKey
    },

    backToHome() {
      this.activeModule = 'home'
    },

    // ─── 部门 ──────────────────────────────────────────────
    async loadAllDingTalkDeptList() {
      const [rootRes, deptRes] = await Promise.all([
        getDeptById(1),
        getAllDeptList()
      ])
      const deptList = deptRes.data || []
      const rootDept = rootRes.data

      if (rootDept && rootDept.deptId !== undefined) {
        const existsRoot = deptList.some(d => String(d.deptId) === String(rootDept.deptId))
        return existsRoot ? deptList : [rootDept, ...deptList]
      }

      return deptList
    },

    async fetchAllDeptList() {
      this.loadingDepts = true
      try {
        const deptList = await this.loadAllDingTalkDeptList()
        if (deptList) {
          this.deptList = deptList
          this.$message.success(`成功获取 ${this.deptList.length} 个部门`)
        } else {
          this.$message.error('获取部门列表失败')
        }
      } catch (e) {
        this.$message.error('请求失败：' + e.message)
      } finally {
        this.loadingDepts = false
      }
    },

    handleDeptSelectionChange(rows) {
      const currentPageIds = new Set(this.pagedDeptList.map(d => d.deptId))
      const otherPageSelected = this.selectedDeptRows.filter(d => !currentPageIds.has(d.deptId))
      this.selectedDeptRows = [...otherPageSelected, ...rows]
      this.isAllDeptSelected = this.selectedDeptRows.length === this.filteredDeptList.length
    },
    // 全选/取消全选所有部门（跨分页）
    toggleSelectAllDepts() {
      if (this.isAllDeptSelected) {
        this.selectedDeptRows = []
        this.isAllDeptSelected = false
      } else {
        this.selectedDeptRows = [...this.filteredDeptList]
        this.isAllDeptSelected = true
      }
    },
    handleDeptSizeChange(size) {
      this.deptPageSize = size
      this.deptCurrentPage = 1
    },
    handleDeptCurrentChange(page) {
      this.deptCurrentPage = page
    },

    async syncSelectedDepts() {
      if (this.selectedDeptRows.length === 0) {
        this.$message.warning('请先勾选需要同步的部门')
        return
      }
      this.syncing = true
      let successCount = 0
      let skipCount = 0
      let failCount = 0
      try {
        const orgRes = await login.getOrgs()
        let existingOrgs = orgRes.data || []
        let pendingDepts = [...this.selectedDeptRows]
        const maxRounds = pendingDepts.length + 1
        let round = 0

        while (pendingDepts.length > 0 && round < maxRounds) {
          round++
          let addedThisRound = 0
          const stillPending = []

          for (const dept of pendingDepts) {
            try {
              const exists = existingOrgs.some(o => String(o.id) === String(dept.deptId))
              if (exists) { skipCount++; continue }

              let parentId = ''
              let parentName = '根节点'

              if (String(dept.deptId) !== '1' && dept.parentId) {
                const parentDingDept = this.deptList.find(d => String(d.deptId) === String(dept.parentId))
                if (!parentDingDept) { failCount++; continue }

                const parentOrg = existingOrgs.find(o =>
                  o.name === parentDingDept.name || String(o.id) === String(parentDingDept.deptId)
                )
                if (!parentOrg) {
                  stillPending.push(dept)
                  continue
                }

                parentId = parentOrg.id
                parentName = parentOrg.name
              }

              const newOrg = {
                id: String(dept.deptId),
                cascadeId: '',
                parentName,
                chairmanId: '',
                parentId,
                name: dept.name,
                status: 0
              }
              const res = await orgs.add(newOrg)
              const newOrgId = res.data?.id || newOrg.id
              existingOrgs.push({ id: newOrgId, name: dept.name, parentId })
              successCount++
              addedThisRound++
            } catch (e) {
              console.error(`[同步] 部门「${dept.name}」失败:`, e)
              failCount++
            }
          }

          if (addedThisRound === 0 && stillPending.length > 0) {
            failCount += stillPending.length
            break
          }

          pendingDepts = stillPending
        }
      } catch (e) {
        this.$message.error('获取现有部门失败：' + e.message)
        return
      } finally {
        this.syncing = false
      }
      this.getOrgTree()
      this.$notify({
        title: '同步完成',
        message: `成功 ${successCount} 个，跳过（已存在）${skipCount} 个，失败（无父部门）${failCount} 个`,
        type: successCount > 0 ? 'success' : 'warning',
        duration: 4000
      })
    },

    getOrgTree() {
      var _this = this
      login.getOrgs().then(response => {
        _this.orgs = response.data.map(item => ({
          id: item.id, label: item.name, parentId: item.parentId || null
        }))
        var orgstmp = JSON.parse(JSON.stringify(_this.orgs))
        _this.orgsTree = listToTreeSelect(orgstmp)
      })
    },

    // ─── 员工 ──────────────────────────────────────────────

    async fetchAllUsers() {
      this.loadingUsers = true
      this.userList = []
      this.fetchProgress = ''

      try {
        // 1. 先确保部门列表已加载
        if (this.deptList.length === 0) {
          this.fetchProgress = '正在获取部门列表...'
          this.deptList = await this.loadAllDingTalkDeptList()
        }

        // 2. 逐个部门查员工
        const userMap = new Map() // userId 去重
        const total = this.deptList.length

        for (let i = 0; i < this.deptList.length; i++) {
          const dept = this.deptList[i]
          this.fetchProgress = `正在获取部门员工 (${i + 1}/${total})：${dept.name}`

          try {
            const res = await getDeptUserList(dept.deptId)
            const list = res.data || []
            list.forEach(u => {
              if (u.userId && !userMap.has(u.userId)) {
                userMap.set(u.userId, u)
              }
            })
          } catch (e) {
            console.warn(`[获取员工] 部门「${dept.name}」失败:`, e.message)
          }
        }

        this.userList = Array.from(userMap.values())
        this.fetchProgress = ''
        this.$message.success(`成功获取 ${this.userList.length} 名员工`)

      } catch (e) {
        this.$message.error('请求失败：' + e.message)
        this.fetchProgress = ''
      } finally {
        this.loadingUsers = false
      }
    },

    // 全选/取消全选所有员工（跨分页）
    toggleSelectAllUsers() {
      if (this.isAllUserSelected) {
        // 取消全选
        this.selectedUserRows = []
        this.isAllUserSelected = false
      } else {
        // 全选 filteredUserList 所有数据
        this.selectedUserRows = [...this.filteredUserList]
        this.isAllUserSelected = true
      }
    },

    // 当前页勾选变化时，合并到 selectedUserRows
    handleUserSelectionChange(rows) {
      // 当前页的数据
      const currentPageIds = new Set(this.pagedUserList.map(u => u.userId))
      // 保留非当前页已选的
      const otherPageSelected = this.selectedUserRows.filter(u => !currentPageIds.has(u.userId))
      // 合并当前页新勾选的
      this.selectedUserRows = [...otherPageSelected, ...rows]
      // 如果手动取消了某行，退出全选状态
      this.isAllUserSelected = this.selectedUserRows.length === this.filteredUserList.length
    },
    handleUserSizeChange(size) {
      this.userPageSize = size
      this.userCurrentPage = 1
    },
    handleUserCurrentChange(page) {
      this.userCurrentPage = page
    },
    // 时间戳转日期
    formatHiredDate(timestamp) {
      if (!timestamp) return '-'
      const date = new Date(timestamp)
      const y = date.getFullYear()
      const m = String(date.getMonth() + 1).padStart(2, '0')
      const d = String(date.getDate()).padStart(2, '0')
      return `${y}-${m}-${d}`
    },

    async syncSelectedUsers() {
      if (this.selectedUserRows.length === 0) {
        this.$message.warning('请先勾选需要同步的员工')
        return
      }

      this.syncingUsers = true
      let successCount = 0
      let skipCount = 0
      let failCount = 0

      try {
        // 1. 获取现有系统用户列表，用于判断重复
        const userRes = await users.getList({ page: 1, limit: 99999 })
        const existingUsers = userRes.data?.list || userRes.data || []

        // 2. 获取现有部门列表，用于匹配 organizationIds
        const orgRes = await login.getOrgs()
        const existingOrgs = orgRes.data || []

        for (const dingUser of this.selectedUserRows) {
          try {
            // 3. account = jobNumber_name
            const account = `${dingUser.jobNumber || ''}_${dingUser.name || ''}`

            // 4. 判断钉钉用户ID是否已存在
            const exists = existingUsers.some(u => String(u.id) === String(dingUser.userId))
            if (exists) {
              console.log(`[同步员工] 钉钉用户ID「${dingUser.userId}」已存在，跳过`)
              skipCount++
              continue
            }

            // 5. 匹配所属部门：钉钉返回的 deptIdList 对应多个部门
            //    通过钉钉部门ID → 找到钉钉部门名称 → 找到系统部门ID
            let organizationIds = ''
            if (dingUser.deptIdList && dingUser.deptIdList.length > 0) {
              const orgIdArr = []
              for (const dingDeptId of dingUser.deptIdList) {
                // 从已查询的钉钉部门列表中找到部门名称
                const dingDept = this.deptList.find(d => String(d.deptId) === String(dingDeptId))
                if (!dingDept) continue
                // 从系统部门中找到对应部门，系统部门ID使用钉钉deptId
                const sysOrg = existingOrgs.find(o => String(o.id) === String(dingDept.deptId))
                if (sysOrg) orgIdArr.push(sysOrg.id)
              }
              organizationIds = orgIdArr.join(',')
            }

            // 6. 没有匹配到任何部门则跳过（后端要求 OrganizationIds 不能为空）
            if (!organizationIds) {
              console.log(`[同步员工] 「${dingUser.name}」未匹配到系统部门，跳过`)
              failCount++
              continue
            }

            
            // 7. 构造新用户数据
            const newUser = {
              id: String(dingUser.userId),
              account: account,
              name: dingUser.name || '',
              password: account || '',
              organizationIds: organizationIds,
              organizations: '',
              parentId: '',
              description: '',
              status: 1,
              sex: 0,
            }

            // 8. 调用接口添加
            const res = await users.add(newUser)
            newUser.id = res.data

            console.log(`[同步员工] 「${dingUser.name}」同步成功，account=${account}`)
            successCount++

          } catch (e) {
            console.error(`[同步员工] 「${dingUser.name}」同步失败:`, e)
            failCount++
          }
        }

      } catch (e) {
        this.$message.error('同步员工失败：' + e.message)
        return
      } finally {
        this.syncingUsers = false
      }

      // 9. 结果提示
      this.$notify({
        title: '同步完成',
        message: `成功 ${successCount} 人，跳过（已存在）${skipCount} 人，失败（无匹配部门）${failCount} 人`,
        type: successCount > 0 ? 'success' : 'warning',
        duration: 4000
      })
    },

    openNotifyUserDialog() {
      this.dialogNotifyUsers = true
    },

    handleConfirmNotifyUsers() {
      if (this.$refs.notifyUserSelector) {
        this.$refs.notifyUserSelector.handleSaveUsers()
      }
      this.dialogNotifyUsers = false
    },

    clearNotifyUsers() {
      this.notificationUserIds = []
      this.notificationUserNames = ''
    },

    async sendDingTalkTextNotification() {
      const content = this.notificationContent.trim()
      if (this.notificationUserIds.length === 0) {
        this.$message.warning('请先选择接收用户')
        return
      }
      if (!content) {
        this.$message.warning('请输入通知内容')
        return
      }

      this.sendingDingTalkNotification = true
      try {
        const res = await sendTextWorkNotification({
          userid_list: this.notificationUserIds.join(','),
          to_all_user: false,
          content
        })
        const dingTaskId = res.data && (res.data.taskId || res.data.task_id)
        const taskId = dingTaskId ? `，任务ID：${dingTaskId}` : ''
        this.$notify({
          title: '发送成功',
          message: `已向 ${this.notificationUserIds.length} 人发送钉钉工作通知${taskId}`,
          type: 'success',
          duration: 4000
        })
        this.notificationContent = ''
      } finally {
        this.sendingDingTalkNotification = false
      }
    },
  },
}
</script>

<style scoped>
.dingtalkmanager-container {
  padding: 20px;
  background: #f0f2f5;
  min-height: 100%;
}

.page-header {
  background: #fff;
  padding: 14px 20px;
  border-radius: 4px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.page-title {
  display: flex;
  align-items: center;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.title-icon {
  font-size: 20px;
  color: #409eff;
  margin-right: 8px;
}

.section-card {
  background: #fff;
  padding: 16px 20px;
  border-radius: 4px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  margin-bottom: 16px;
}

.module-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 16px;
}

.module-card {
  background: #fff;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  padding: 20px;
  min-height: 118px;
  display: flex;
  align-items: center;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s, transform 0.2s;
}

.module-card:hover {
  border-color: #409eff;
  box-shadow: 0 6px 18px rgba(64, 158, 255, 0.14);
  transform: translateY(-2px);
}

.module-icon {
  width: 48px;
  height: 48px;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 14px;
  color: #fff;
  font-size: 24px;
}

.module-icon.sync {
  background: #409eff;
}

.module-icon.notify {
  background: #67c23a;
}

.module-content {
  min-width: 0;
  flex: 1;
}

.module-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 8px;
}

.module-desc {
  font-size: 13px;
  color: #606266;
  line-height: 20px;
}

.module-arrow {
  color: #c0c4cc;
  margin-left: 12px;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.section-title {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.section-title i {
  margin-right: 6px;
  color: #409eff;
}

.section-actions {
  display: flex;
  gap: 10px;
}

.search-bar {
  margin-top: 12px;
}

.table-footer {
  margin-top: 12px;
  font-size: 13px;
  color: #606266;
  text-align: right;
}

.pagination-bar {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
}

.fetch-progress {
  margin-top: 12px;
  font-size: 13px;
  color: #409eff;
  padding: 8px 12px;
  background: #ecf5ff;
  border-radius: 4px;
}

.notify-form {
  margin-top: 16px;
  max-width: 820px;
}

.notify-user-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.notify-user-count {
  font-size: 13px;
  color: #606266;
}

.notify-selected-users {
  margin-top: 8px;
  padding: 8px 10px;
  background: #f5f7fa;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  color: #303133;
  line-height: 20px;
  max-height: 80px;
  overflow: auto;
}
</style>
