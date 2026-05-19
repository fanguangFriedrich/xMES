<template>
  <div class="detail-container" v-loading="loading">
    <!-- 顶部返回栏 -->
    <div class="detail-header no-print">
      <el-button size="small" icon="el-icon-arrow-left" @click="goBack">返回列表</el-button>
      <span class="detail-title">派工单详情</span>
      <div style="margin-left: auto; display: flex; gap: 8px;">
        <el-button size="small" icon="el-icon-download" @click="handleExport" :disabled="!detail">导出 Excel</el-button>
        <el-button size="small" icon="el-icon-printer" @click="handlePrint" :disabled="!detail">打印</el-button>
      </div>
    </div>

    <template v-if="detail">
      <!-- 基本信息卡片 -->
      <div class="detail-card no-print">
        <div class="card-title">基本信息</div>
        <el-descriptions :column="4" border size="small">
          <el-descriptions-item label="打开时间">{{ formatTime(detail.mesWorkOrderDto.createdTime) }}</el-descriptions-item>
          <el-descriptions-item label="派工人">{{ detail.mesWorkOrderDto.dispatchWorkers || '--' }}</el-descriptions-item>
          <el-descriptions-item label="派工单号">{{ detail.mesWorkOrderDto.workOrder || '--' }}</el-descriptions-item>
          <el-descriptions-item label="承诺到料日期">{{ formatDate(detail.mesWorkOrderDto.promiseDeliveryDate) }}</el-descriptions-item>
          <el-descriptions-item label="计划开始时间">{{ formatDate(detail.mesWorkOrderDto.startDate) }}</el-descriptions-item>
          <el-descriptions-item label="计划结束时间">{{ formatDate(detail.mesWorkOrderDto.endDate) }}</el-descriptions-item>
          <el-descriptions-item label="签收码">
            <el-image style="width: 96px; height: 96px" :src="qrCodeUrl" fit="contain">
              <div slot="error" class="image-slot"><i class="el-icon-picture-outline"></i></div>
            </el-image>
          </el-descriptions-item>
          <el-descriptions-item label="是否加急">{{ detail.mesWorkOrderDto.status === '1' ? '是' : '--' }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <!-- 工序信息卡片 -->
      <div class="detail-card no-print" style="margin-top: 16px;">
        <div class="card-title">工序信息</div>
        <el-table :data="detail.mesWorkOrderProcessDtos" border size="small" style="width: 100%">
          <el-table-column prop="processItemNo" label="工序" width="80" />
          <el-table-column prop="processText" label="工艺" width="140" />
          <el-table-column prop="workCenterName" label="工作中心" width="160" />
          <el-table-column prop="departmentName" label="组别" width="180" />
          <el-table-column prop="productionValue" label="产值" width="100" />
          <el-table-column label="条码" width="120">
            <template slot-scope="scope">{{ scope.row.workOrderCode || '--' }}</template>
          </el-table-column>
          <el-table-column label="计划开始时" width="160">
            <template slot-scope="scope">{{ formatTime(scope.row.sapCreatedTime) }}</template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 生产订单卡片 -->
      <div class="detail-card no-print" style="margin-top: 16px;">
        <div class="card-title">生产订单</div>
        <el-table :data="detail.mesPoDtos" border size="small" style="width: 100%">
          <el-table-column prop="productionOrderNo" label="生产订单" width="160" />
          <el-table-column prop="drawNo" label="图纸编号" width="200" />
          <el-table-column prop="materialDescription" label="图纸名称" min-width="200" show-overflow-tooltip />
          <el-table-column prop="drawRevision" label="图纸版本" width="100" />
          <el-table-column prop="quantity" label="数量" width="80" />
          <el-table-column prop="materialNo" label="产品条码" width="160" />
          <el-table-column label="操作" width="100" fixed="right">
            <template slot-scope="scope">
              <el-button type="text" size="small" @click="viewPoDetail(scope.row)">查看</el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </template>

    <!-- ===== 打印专用区域 ===== -->
    <div id="detail-print-area" v-if="detail">
      <!-- 标题 -->
      <div class="print-header">
        <h2>派工单详情</h2>
        <p class="print-meta">打印时间：{{ printTime }}</p>
      </div>

      <!-- 基本信息 -->
      <div class="print-section-title">基本信息</div>
      <table class="print-info-table">
        <tr>
          <td class="info-label">打开时间</td>
          <td>{{ formatTime(detail.mesWorkOrderDto.createdTime) }}</td>
          <td class="info-label">派工人</td>
          <td>{{ detail.mesWorkOrderDto.dispatchWorkers || '--' }}</td>
        </tr>
        <tr>
          <td class="info-label">派工单号</td>
          <td>{{ detail.mesWorkOrderDto.workOrder || '--' }}</td>
          <td class="info-label">承诺到料日期</td>
          <td>{{ formatDate(detail.mesWorkOrderDto.promiseDeliveryDate) }}</td>
        </tr>
        <tr>
          <td class="info-label">计划开始时间</td>
          <td>{{ formatDate(detail.mesWorkOrderDto.startDate) }}</td>
          <td class="info-label">计划结束时间</td>
          <td>{{ formatDate(detail.mesWorkOrderDto.endDate) }}</td>
        </tr>
        <tr>
          <td class="info-label">是否加急</td>
          <td>{{ detail.mesWorkOrderDto.status === '1' ? '是' : '--' }}</td>
          <td class="info-label">签收码</td>
          <td>
            <img v-if="qrCodeUrl" :src="qrCodeUrl" style="width:80px;height:80px;" />
            <span v-else>--</span>
          </td>
        </tr>
      </table>

      <!-- 工序信息 -->
      <div class="print-section-title" style="margin-top: 14px;">工序信息</div>
      <table class="print-table">
        <thead>
          <tr>
            <th>工序</th>
            <th>工艺</th>
            <th>工作中心</th>
            <th>组别</th>
            <th>产值</th>
            <th>条码</th>
            <th>计划开始时</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, i) in detail.mesWorkOrderProcessDtos" :key="'p'+i">
            <td>{{ row.processItemNo }}</td>
            <td>{{ row.processText }}</td>
            <td>{{ row.workCenterName }}</td>
            <td>{{ row.departmentName }}</td>
            <td>{{ row.productionValue }}</td>
            <td>{{ row.workOrderCode || '--' }}</td>
            <td>{{ formatTime(row.sapCreatedTime) }}</td>
          </tr>
        </tbody>
      </table>

      <!-- 生产订单 -->
      <div class="print-section-title" style="margin-top: 14px;">生产订单</div>
      <table class="print-table">
        <thead>
          <tr>
            <th>生产订单</th>
            <th>图纸编号</th>
            <th>图纸名称</th>
            <th>图纸版本</th>
            <th>数量</th>
            <th>产品条码</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, i) in detail.mesPoDtos" :key="'o'+i">
            <td>{{ row.productionOrderNo }}</td>
            <td>{{ row.drawNo }}</td>
            <td>{{ row.materialDescription }}</td>
            <td>{{ row.drawRevision }}</td>
            <td>{{ row.quantity }}</td>
            <td>{{ row.materialNo }}</td>
          </tr>
        </tbody>
      </table>
    </div>

  </div>
</template>

<script>
import { checkMesWorkOrder, getMesWorkOrderDetail } from '@/api/mesWorkOrders'
import * as XLSX from 'xlsx'

export default {
  name: 'mesWorkOrderDetail',

  data() {
    return {
      workOrder: this.$route.query.workOrder || '',
      loading: false,
      detail: null,
      qrCodeUrl: '',
      printTime: ''
    }
  },

  mounted() {
    if (this.workOrder) {
      this.loadDetail()
    } else {
      this.$message.error('派工单号不能为空')
    }
  },

  methods: {
    async loadDetail() {
      this.loading = true
      try {
        const checkRes = await checkMesWorkOrder(this.workOrder)
        if (!checkRes.data) {
          this.$message.error(checkRes.message || '派工单不存在')
          return
        }
        const detailRes = await getMesWorkOrderDetail(this.workOrder)
        if (detailRes.code === 200) {
          this.detail = detailRes.data
        } else {
          this.$message.error(detailRes.message || '获取详情失败')
        }
      } catch (e) {
        this.$message.error('请求失败：' + e.message)
      } finally {
        this.loading = false
      }
    },

    // ===== 导出 Excel（三个 Sheet） =====
    handleExport() {
      if (!this.detail) return
      const dto = this.detail.mesWorkOrderDto
      const wb = XLSX.utils.book_new()

      // Sheet1：基本信息
      const baseData = [
        ['字段', '值'],
        ['打开时间',        this.formatTime(dto.createdTime)],
        ['派工人',          dto.dispatchWorkers || '--'],
        ['派工单号',        dto.workOrder || '--'],
        ['承诺到料日期',    this.formatDate(dto.promiseDeliveryDate)],
        ['计划开始时间',    this.formatDate(dto.startDate)],
        ['计划结束时间',    this.formatDate(dto.endDate)],
        ['是否加急',        dto.status === '1' ? '是' : '--']
      ]
      const ws1 = XLSX.utils.aoa_to_sheet(baseData)
      ws1['!cols'] = [{ wch: 16 }, { wch: 28 }]
      XLSX.utils.book_append_sheet(wb, ws1, '基本信息')

      // Sheet2：工序信息
      const processData = (this.detail.mesWorkOrderProcessDtos || []).map(r => ({
        '工序': r.processItemNo,
        '工艺': r.processText,
        '工作中心': r.workCenterName,
        '组别': r.departmentName,
        '产值': r.productionValue,
        '条码': r.workOrderCode || '--',
        '计划开始时': this.formatTime(r.sapCreatedTime)
      }))
      const ws2 = XLSX.utils.json_to_sheet(processData)
      ws2['!cols'] = [{ wch: 8 }, { wch: 14 }, { wch: 16 }, { wch: 18 }, { wch: 10 }, { wch: 14 }, { wch: 18 }]
      XLSX.utils.book_append_sheet(wb, ws2, '工序信息')

      // Sheet3：生产订单
      const poData = (this.detail.mesPoDtos || []).map(r => ({
        '生产订单': r.productionOrderNo,
        '图纸编号': r.drawNo,
        '图纸名称': r.materialDescription,
        '图纸版本': r.drawRevision,
        '数量': r.quantity,
        '产品条码': r.materialNo
      }))
      const ws3 = XLSX.utils.json_to_sheet(poData)
      ws3['!cols'] = [{ wch: 16 }, { wch: 20 }, { wch: 28 }, { wch: 12 }, { wch: 8 }, { wch: 16 }]
      XLSX.utils.book_append_sheet(wb, ws3, '生产订单')

      const fileName = `派工单详情_${dto.workOrder || ''}_${this.today()}.xlsx`
      XLSX.writeFile(wb, fileName)
      this.$message.success('导出成功')
    },

    // ===== 打印 =====
    handlePrint() {
      if (!this.detail) return
      const now = new Date()
      const pad = n => String(n).padStart(2, '0')
      this.printTime = `${now.getFullYear()}-${pad(now.getMonth()+1)}-${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}`
      this.$nextTick(() => window.print())
    },

    today() {
      const d = new Date()
      const pad = n => String(n).padStart(2, '0')
      return `${d.getFullYear()}${pad(d.getMonth()+1)}${pad(d.getDate())}`
    },

    formatDate(timestamp) {
      if (!timestamp) return '--'
      const d = new Date(timestamp)
      return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`
    },

    formatTime(timestamp) {
      if (!timestamp) return '--'
      const d = new Date(timestamp)
      return `${this.formatDate(timestamp)} ${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`
    },

    viewPoDetail(row) {
      this.$router.push({ path: '/mes/po-detail', query: { productionOrderNo: row.productionOrderNo } })
    },

    goBack() {
      this.$router.back()
    }
  }
}
</script>

<style scoped>
.detail-container {
  padding: 20px;
  background: #f0f2f5;
  min-height: 100%;
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 16px;
  background: #fff;
  padding: 14px 20px;
  border-radius: 4px;
  margin-bottom: 16px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.detail-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.detail-card {
  background: #fff;
  border-radius: 4px;
  padding: 20px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 16px;
  padding-left: 10px;
  border-left: 3px solid #409eff;
}

.image-slot {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  height: 100%;
  background: #f5f7fa;
  color: #909399;
  font-size: 28px;
}

/* 打印区域屏幕上隐藏 */
#detail-print-area {
  display: none;
}
</style>

<!-- 全局打印样式，不能加 scoped -->
<style>
@media print {
  /* 第一步：隐藏所有 */
  body > * {
    display: none !important;
  }

  /* 第二步：逐层显示到 detail-container */
  body,
  body > #app,
  body > #app > .app-wrapper,
  body > #app > .app-wrapper > .el-container,
  body > #app > .app-wrapper > .el-container > .el-container,
  body > #app > .app-wrapper > .el-container > .el-container > .main-container,
  body > #app > .app-wrapper > .el-container > .el-container > .main-container > .app-main,
  body > #app > .app-wrapper > .el-container > .el-container > .main-container > .app-main > .detail-container {
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

  /* 第三步：隐藏框架残余元素 */
  .el-header,
  .sidebar-container,
  .tags-view-container,
  .tags-view-wrapper {
    display: none !important;
  }

  /* 第四步：隐藏页面正文，只显示打印区域 */
  .no-print {
    display: none !important;
  }

  #detail-print-area {
    display: block !important;
  }

  /* ── 打印内容样式 ── */
  .print-header {
    text-align: center;
    margin-bottom: 14px;
    padding-bottom: 10px;
    border-bottom: 2px solid #333;
  }

  .print-header h2 {
    font-size: 18pt;
    margin: 0 0 4px 0;
    color: #000;
  }

  .print-meta {
    font-size: 9pt;
    color: #555;
    margin: 0;
  }

  .print-section-title {
    font-size: 11pt;
    font-weight: bold;
    color: #000;
    margin-bottom: 6px;
    padding-left: 8px;
    border-left: 3px solid #333;
  }

  /* 基本信息键值表 */
  .print-info-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 9pt;
  }

  .print-info-table td {
    border: 1px solid #999;
    padding: 5px 8px;
  }

  .print-info-table .info-label {
    background-color: #e8e8e8 !important;
    font-weight: bold;
    width: 14%;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }

  /* 工序/生产订单表格 */
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

  @page {
    size: A4 landscape;
    margin: 15mm 10mm;
  }
}
</style>