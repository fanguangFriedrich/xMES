<template>
  <div class="mes-container">
    <!-- 筛选区域 -->
    <div class="filter-panel">
      <div class="filter-row">
        <div class="filter-item">
          <label>派工单号</label>
          <el-input
            v-model="queryParams.WorkOrder"
            placeholder="请输入派工单号"
            clearable
            size="small"
          />
        </div>
        <div class="filter-item">
          <label>派工人员工号</label>
          <el-input
            v-model="queryParams.DispatchWorkerJobNo"
            placeholder="请输入工号"
            clearable
            size="small"
          />
        </div>
        <div class="filter-item">
          <label>开始日期范围</label>
          <el-date-picker
            v-model="startDateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            size="small"
            value-format="timestamp"
            @change="onStartDateChange"
          />
        </div>
        <div class="filter-item">
          <label>结束日期范围</label>
          <el-date-picker
            v-model="endDateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            size="small"
            value-format="timestamp"
            @change="onEndDateChange"
          />
        </div>
        <div class="filter-item">
          <label>状态</label>
          <el-select v-model="queryParams.Status1" placeholder="未完成" size="small" clearable>
            <el-option label="未完成" :value="0" />
            <el-option label="已完成" :value="1" />
          </el-select>
        </div>
        <div class="filter-item filter-actions">
          <el-button type="primary" size="small" icon="el-icon-search" @click="handleSearch">查询</el-button>
          <el-button size="small" icon="el-icon-refresh" @click="handleReset">重置</el-button>
          <el-button size="small" icon="el-icon-download" @click="handleExport">导出 Excel</el-button>
          <el-button size="small" icon="el-icon-printer" @click="handlePrint">打印</el-button>
        </div>
      </div>
    </div>

    <!-- 表格区域 -->
    <div class="table-wrapper">
      <el-table
        :data="tableData"
        v-loading="loading"
        style="width: 100%"
        stripe
        border
        size="small"
        :header-cell-style="{ background: '#f5f7fa', color: '#606266', fontWeight: 600 }"
      >
        <el-table-column type="index" label="序号" width="60" align="center" />
        <el-table-column prop="workOrder" label="派工单号" min-width="160" />
        <el-table-column prop="workOrderSerialNo" label="派工单流水码" min-width="160" />
        <el-table-column prop="dispatchWorkers" label="派工人员" min-width="120" />
        <el-table-column prop="startDate" label="开始日期" min-width="140" align="center">
          <template slot-scope="{ row }">
            {{ formatDate(row.startDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="endDate" label="结束日期" min-width="140" align="center">
          <template slot-scope="{ row }">
            {{ formatDate(row.endDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="getStatusType(row.status1)" size="mini">{{ getStatusString(row.status1) || '-' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="创建时间" min-width="150" align="center">
          <template slot-scope="{ row }">
            {{ formatDate(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="createBy" label="创建人" min-width="100" align="center" />
        <el-table-column label="操作" width="100" align="center" fixed="right">
          <template slot-scope="{ row }">
            <el-button type="text" size="small" @click="handleDetail(row)">查看详情</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-wrapper">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next, jumper"
          :total="total"
          :page-size="queryParams.PageSize"
          :current-page="queryParams.PageNum"
          :page-sizes="[10, 20, 50, 100]"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </div>

    <!-- ===== 打印专用区域（屏幕上隐藏，打印时显示） ===== -->
    <div id="print-area">
      <div class="print-header">
        <h2>派工单列表</h2>
        <p class="print-meta">
          打印时间：{{ printTime }}
          <span v-if="queryParams.Status1 !== ''">
            &nbsp;&nbsp;状态：{{ getStatusString(queryParams.Status1) }}
          </span>
        </p>
      </div>
      <table class="print-table">
        <thead>
          <tr>
            <th>序号</th>
            <th>派工单号</th>
            <th>派工单流水码</th>
            <th>派工人员</th>
            <th>开始日期</th>
            <th>结束日期</th>
            <th>状态</th>
            <th>创建时间</th>
            <th>创建人</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, index) in tableData" :key="index">
            <td>{{ index + 1 }}</td>
            <td>{{ row.workOrder }}</td>
            <td>{{ row.workOrderSerialNo }}</td>
            <td>{{ row.dispatchWorkers }}</td>
            <td>{{ formatDate(row.startDate) }}</td>
            <td>{{ formatDate(row.endDate) }}</td>
            <td>{{ getStatusString(row.status1) }}</td>
            <td>{{ formatDate(row.createdTime) }}</td>
            <td>{{ row.createBy }}</td>
          </tr>
        </tbody>
      </table>
      <div class="print-footer">
        共 {{ tableData.length }} 条记录（当前页）
      </div>
    </div>

  </div>
</template>

<script>
import { getMesWorkOrderPage } from '@/api/mesWorkOrders'
// 注意：需要安装 xlsx 库：npm install xlsx
import * as XLSX from 'xlsx'

const DEFAULT_WORK_CENTERS = [
  'CCK01', 'CCK02', 'CCK03', 'CNC00', 'DCC00', 'DG01', 'DG02', 'DG03',
  'JGZX00', 'JHJ02', 'JHJ03', 'JJC00', 'JPG01', 'JPG04', 'JSH02', 'JSH04',
  'JZZ05', 'JZZ06', 'PQ00', 'QBJ01', 'QGJ01', 'SKCC00', 'SQG00', 'XC00',
  'XCC00', 'XQG00', 'ZJK00', 'ZW01'
]

export default {
  name: 'mesWorkOrder',

  data() {
    return {
      loading: false,
      tableData: [],
      total: 0,
      startDateRange: null,
      endDateRange: null,
      printTime: '',
      queryParams: {
        WorkOrder: '',
        WorkCenters: [...DEFAULT_WORK_CENTERS],
        DispatchWorkerJobNo: '',
        StartDateRange: {
          StartDate: null,
          EndDate: null
        },
        EndDateRange: {
          StartDate: null,
          EndDate: null
        },
        PageSize: 10,
        PageNum: 1,
        Status1: 0,
        IsAdmin: true
      }
    }
  },

  created() {
    this.fetchData()
  },

  methods: {
    async fetchData() {
      this.loading = true
      try {
        const res = await getMesWorkOrderPage(this.buildParams())
        if (res && res.data) {
          const inner = res.data.data || res.data
          this.tableData = Array.isArray(inner) ? inner : (inner.data || [])
          this.total = res.data.count || (inner.count) || 0
        }
      } catch (e) {
        this.$message && this.$message.error('获取派工单数据失败')
        console.error(e)
      } finally {
        this.loading = false
      }
    },

    buildParams() {
      const p = { ...this.queryParams }

      if (!p.WorkOrder) delete p.WorkOrder
      if (!p.DispatchWorkerJobNo) delete p.DispatchWorkerJobNo
      if (!p.StartDateRange.StartDate) delete p.StartDateRange
      if (!p.EndDateRange.StartDate) delete p.EndDateRange

      return p
    },

    onStartDateChange(val) {
      if (val) {
        this.queryParams.StartDateRange.StartDate = val[0]
        this.queryParams.StartDateRange.EndDate = val[1]
      } else {
        this.queryParams.StartDateRange = { StartDate: null, EndDate: null }
      }
    },

    onEndDateChange(val) {
      if (val) {
        this.queryParams.EndDateRange.StartDate = val[0]
        this.queryParams.EndDateRange.EndDate = val[1]
      } else {
        this.queryParams.EndDateRange = { StartDate: null, EndDate: null }
      }
    },

    handleSearch() {
      this.queryParams.PageNum = 1
      this.fetchData()
    },

    handleReset() {
      this.queryParams.WorkOrder = ''
      this.queryParams.DispatchWorkerJobNo = ''
      this.queryParams.Status1 = 0
      this.queryParams.PageNum = 1
      this.queryParams.StartDateRange = { StartDate: null, EndDate: null }
      this.queryParams.EndDateRange = { StartDate: null, EndDate: null }
      this.startDateRange = null
      this.endDateRange = null
      this.fetchData()
    },

    handleSizeChange(val) {
      this.queryParams.PageSize = val
      this.queryParams.PageNum = 1
      this.fetchData()
    },

    handleCurrentChange(val) {
      this.queryParams.PageNum = val
      this.fetchData()
    },

    handleDetail(row) {
      this.$router.push({
        path: '/mes/mesWorkOrder/detail',
        query: {
          workOrder: row.workOrder
        }
      })
    },

    // ===== 导出 Excel =====
    handleExport() {
      if (!this.tableData.length) {
        this.$message.warning('暂无数据可导出')
        return
      }

      // 构造导出数据（当前页）
      const exportData = this.tableData.map((row, index) => ({
        '序号': index + 1,
        '派工单号': row.workOrder || '',
        '派工单流水码': row.workOrderSerialNo || '',
        '派工人员': row.dispatchWorkers || '',
        '开始日期': this.formatDate(row.startDate),
        '结束日期': this.formatDate(row.endDate),
        '状态': this.getStatusString(row.status1),
        '创建时间': this.formatDate(row.createdTime),
        '创建人': row.createBy || ''
      }))

      const ws = XLSX.utils.json_to_sheet(exportData)

      // 设置列宽
      ws['!cols'] = [
        { wch: 6 },   // 序号
        { wch: 18 },  // 派工单号
        { wch: 18 },  // 流水码
        { wch: 12 },  // 派工人员
        { wch: 14 },  // 开始日期
        { wch: 14 },  // 结束日期
        { wch: 10 },  // 状态
        { wch: 16 },  // 创建时间
        { wch: 12 }   // 创建人
      ]

      const wb = XLSX.utils.book_new()
      XLSX.utils.book_append_sheet(wb, ws, '派工单列表')

      const now = new Date()
      const fileName = `派工单列表_${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}.xlsx`
      XLSX.writeFile(wb, fileName)
      this.$message.success('导出成功')
    },

    // ===== 打印 =====
    handlePrint() {
      if (!this.tableData.length) {
        this.$message.warning('暂无数据可打印')
        return
      }
      // 更新打印时间
      const now = new Date()
      const pad = n => String(n).padStart(2, '0')
      this.printTime = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}`

      this.$nextTick(() => {
        window.print()
      })
    },

    formatDate(ts) {
      if (!ts) return '-'
      const d = new Date(ts)
      const pad = n => String(n).padStart(2, '0')
      return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
    },

    getStatusType(status1) {
      const map = { 0: 'warning', 1: 'success' }
      return map[status1] || 'info'
    },

    getStatusString(status1) {
      const map = { 0: '未完成', 1: '已完成' }
      return map[status1] || '未知'
    }
  }
}
</script>

<style scoped>
.mes-container {
  padding: 20px;
  background: #f0f2f5;
  min-height: 100%;
}

.filter-panel {
  background: #fff;
  border-radius: 4px;
  padding: 18px 20px 10px;
  margin-bottom: 16px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.filter-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  align-items: flex-end;
}

.filter-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.filter-item label {
  font-size: 13px;
  color: #606266;
  font-weight: 500;
}

.filter-actions {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  padding-bottom: 0;
}

.table-wrapper {
  background: #fff;
  border-radius: 4px;
  padding: 16px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.pagination-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

/* ===== 打印区域：屏幕上隐藏 ===== */
#print-area {
  display: none;
}
</style>

<!-- 全局打印样式（不能 scoped，否则选不到框架层级的元素） -->
<style>
@media print {
  /* ── 第一步：隐藏 body 下所有直接子元素 ── */
  body > * {
    display: none !important;
  }

  /* ── 第二步：把 #print-area 的每一层祖先逐级设为可见 ──
       路径：body > #app > .app-wrapper > section.el-container(垂直) >
             section.el-container(水平) > .main-container > .app-main >
             .mes-container > #print-area
  */
  body,
  body > #app,
  body > #app > .app-wrapper,
  body > #app > .app-wrapper > .el-container,
  body > #app > .app-wrapper > .el-container > .el-container,
  body > #app > .app-wrapper > .el-container > .el-container > .main-container,
  body > #app > .app-wrapper > .el-container > .el-container > .main-container > .app-main,
  body > #app > .app-wrapper > .el-container > .el-container > .main-container > .app-main > .mes-container {
    display: block !important;
    overflow: visible !important;
    width: 100% !important;
    height: auto !important;
    min-height: unset !important;
    position: static !important;
    padding: 0 !important;
    margin: 0 !important;
    background: #fff !important;
    box-shadow: none !important;
    border: none !important;
  }

  /* ── 第三步：隐藏框架残余元素 ── */
  .el-header,
  .sidebar-container,
  .tags-view-container,
  .tags-view-wrapper {
    display: none !important;
  }

  /* ── 第四步：隐藏 mes-container 内除打印区域外的元素 ── */
  .mes-container > .filter-panel,
  .mes-container > .table-wrapper {
    display: none !important;
  }

  /* ── 第四步：显示打印区域 ── */
  #print-area {
    display: block !important;
  }

  /* ── 打印内容样式 ── */
  .print-header {
    text-align: center;
    margin-bottom: 16px;
    padding-bottom: 10px;
    border-bottom: 2px solid #333;
  }

  .print-header h2 {
    font-size: 18pt;
    margin: 0 0 6px 0;
    color: #000;
  }

  .print-meta {
    font-size: 9pt;
    color: #555;
    margin: 0;
  }

  .print-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 9pt;
    color: #000;
  }

  .print-table th,
  .print-table td {
    border: 1px solid #999;
    padding: 5px 8px;
    text-align: left;
    word-break: break-all;
  }

  .print-table th {
    background-color: #e8e8e8 !important;
    font-weight: bold;
    text-align: center;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }

  .print-table tbody tr:nth-child(even) td {
    background-color: #f8f8f8 !important;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }

  .print-footer {
    margin-top: 10px;
    font-size: 9pt;
    color: #555;
    text-align: right;
  }

  @page {
    size: A4 landscape;
    margin: 15mm 10mm;
  }
}
</style>
