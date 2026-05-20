<template>
  <div class="smart-lamp-page">
    <div class="lamp-tabs">
      <span
        v-for="tab in tabs"
        :key="tab.key"
        class="lamp-tab"
        :class="{ active: activeTab === tab.key }"
        @click="activeTab = tab.key"
      >{{ tab.label }}</span>
    </div>

    <div v-if="viewMode === 'oee'" class="oee-page" v-loading="oeeLoading">
      <div class="oee-topbar">
        <span class="oee-breadcrumb active" @click="backToList">三色灯</span>
        <span class="oee-breadcrumb">OEE时序</span>
        <span class="oee-label">查询方式:</span>
        <el-radio-group v-model="oeeQueryType" size="mini">
          <el-radio-button label="day">日查询</el-radio-button>
        </el-radio-group>
        <el-date-picker
          v-model="oeeDate"
          size="mini"
          class="oee-date"
          type="date"
          value-format="yyyy-MM-dd"
          placeholder="选择日期"
          @change="loadOeeData"
        />
        <div class="oee-spacer"></div>
        <el-button size="mini" icon="el-icon-download" circle @click="downloadOeeData" />
        <el-button size="mini" type="text" icon="el-icon-back" @click="backToList">返回</el-button>
      </div>

      <div class="oee-timeline-panel">
        <div class="oee-panel-title">OEE时序</div>
        <div class="timeline-device">{{ oeeDeviceName }}</div>
        <div class="day-track">
          <span
            v-for="(segment, index) in oeeTimelineSegments"
            :key="`${segment.startTimeText}-${index}`"
            class="day-segment"
            :style="{ left: segment.left + '%', width: segment.width + '%', background: segment.color }"
            :title="`${segment.stateName} ${segment.durationText}`"
          ></span>
        </div>
        <div class="day-scale">
          <span>00:00</span>
          <span>04:00</span>
          <span>08:00</span>
          <span>12:00</span>
          <span>16:00</span>
          <span>20:00</span>
          <span>24:00</span>
        </div>
      </div>

      <div class="oee-content">
        <div class="oee-detail-card">
          <div class="oee-card-head">
            <span>OEE时序详情</span>
            <div>
              <el-button size="mini" type="primary" plain>时间补偿</el-button>
              <el-select v-model="oeeReasonFilter" size="mini" clearable placeholder="请选择" class="reason-select">
                <el-option label="全部" value="" />
                <el-option label="无" value="无" />
              </el-select>
            </div>
          </div>
          <el-table :data="oeeRows" size="mini" height="320" class="oee-table">
            <el-table-column prop="startTimeText" label="开始时间" min-width="128" />
            <el-table-column label="状态" min-width="70">
              <template slot-scope="{ row }">
                <span class="state-dot" :style="{ background: row.color }"></span>{{ row.stateName }}
              </template>
            </el-table-column>
            <el-table-column prop="durationText" label="运行时长" min-width="92" sortable />
            <el-table-column prop="reason" label="原因" min-width="70" />
            <el-table-column prop="operator" label="操作人" min-width="90" />
            <el-table-column label="操作" width="78">
              <template>
                <el-button size="mini" type="primary" plain round>编辑</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <div class="oee-chart-card">
          <div class="chart-title">当日时长分布</div>
          <div ref="durationChart" class="chart-box"></div>
        </div>

        <div class="oee-chart-card">
          <div class="chart-title">异常原因分布</div>
          <div ref="reasonChart" class="chart-box"></div>
        </div>
      </div>
    </div>

    <template v-else>
    <div class="lamp-toolbar">
      <el-input
        v-model="query.projectName"
        size="small"
        clearable
        class="project-input"
        placeholder="请输入项目名称"
        @keyup.enter.native="loadLampData"
      />
      <el-select
        v-model="query.groupName"
        size="small"
        clearable
        filterable
        placeholder="默认总线"
        class="group-select"
        @change="loadLampData"
      >
        <el-option
          v-for="item in groupOptions"
          :key="item"
          :label="item"
          :value="item"
        />
      </el-select>
      <el-input
        v-model="query.dtuSn"
        size="small"
        clearable
        class="sn-input"
        placeholder="请输入序列号"
        @keyup.enter.native="loadLampData"
      />
      <el-date-picker
        v-model="query.date"
        size="small"
        class="date-input"
        type="date"
        value-format="yyyy-MM-dd"
        placeholder="选择日期"
        @change="loadLampData"
      />
      <el-button
        size="small"
        type="primary"
        icon="el-icon-search"
        :loading="loading"
        @click="loadLampData"
      >查询</el-button>
      <el-button
        size="small"
        icon="el-icon-refresh"
        :loading="loading"
        @click="resetQuery"
      >刷新</el-button>
      <div class="toolbar-spacer"></div>
      <span class="refresh-tip">最近更新: {{ lastRefreshText }}</span>
      <el-select
        v-model="refreshInterval"
        size="small"
        class="refresh-select"
        :disabled="autoRefresh"
        @change="restartAutoRefresh"
      >
        <el-option label="8秒" :value="8000" />
        <el-option label="15秒" :value="15000" />
        <el-option label="30秒" :value="30000" />
        <el-option label="60秒" :value="60000" />
      </el-select>
      <el-switch
        v-model="autoRefresh"
        active-text="自动刷新"
        @change="handleAutoRefreshChange"
      />
    </div>

    <div class="status-row">
      <span
        v-for="item in legendItems"
        :key="item.key"
        class="legend-item"
        :class="{ active: stateFilter === item.key }"
        @click="stateFilter = item.key"
      >
        <i class="legend-color" :style="{ background: item.color }"></i>
        {{ item.label }}:{{ item.count }}台
      </span>
    </div>

    <div v-loading="loading" class="lamp-board">
      <el-empty
        v-if="!loading && filteredCards.length === 0"
        description="暂无智能灯数据"
      />
      <div
        v-for="card in filteredCards"
        :key="card.dtuSn"
        class="lamp-card"
        :class="card.stateClass"
      >
        <div class="card-glow"></div>
        <div class="card-title">
          <span class="device-name">{{ card.deviceName || card.dtuSn }}</span>
          <span class="state-pill">{{ card.stateName }}</span>
        </div>

        <div class="card-body">
          <div class="tower">
            <span
              v-for="light in lightStack"
              :key="light.key"
              class="tower-light"
              :class="{ on: card.stateKey === light.key }"
              :style="getTowerStyle(card, light)"
            ></span>
          </div>

          <div class="card-main">
            <div class="metric-label">稼动率</div>
            <div class="metric-value">{{ card.rateText }}</div>
            <div class="current-time">{{ card.currentDurationLabel }}: {{ card.currentDurationText }}</div>
            <div class="counter-box">{{ card.counterText }}</div>
          </div>
        </div>

        <div class="card-meta">
          <span>状态: {{ card.stateName }}</span>
          <span>{{ formatTime(card.startTime) }}</span>
        </div>

        <div class="card-actions">
          <el-button size="mini" icon="el-icon-data-line" @click="openOeeTimeline(card)">OEE时序</el-button>
          <el-button size="mini" icon="el-icon-setting" @click="openSetting(card)">设置</el-button>
          <el-button size="mini" icon="el-icon-s-marketing" @click="openDetail(card)">稼动率</el-button>
          <el-button size="mini" icon="el-icon-document" @click="openCounter(card)">计数明细</el-button>
        </div>
      </div>
    </div>

    <el-dialog
      :title="detailTitle"
      :visible.sync="detailVisible"
      width="620px"
    >
      <el-table :data="detailRows" size="small" border>
        <el-table-column prop="name" label="项目" min-width="120" />
        <el-table-column prop="value" label="数值" min-width="120" />
      </el-table>
    </el-dialog>
    </template>
  </div>
</template>

<script>
import echarts from 'echarts'
import {
  getDtuSnData,
  getUserGroupDtuSns,
  getRealtimeSnapshot
} from '@/api/tricolorlamp'

const STATE_META = {
  all: { label: '全部', color: '#111827', className: 'state-all' },
  red: { label: '红灯', color: '#ef233c', className: 'state-red' },
  yellow: { label: '黄灯', color: '#f59f00', className: 'state-yellow' },
  green: { label: '绿灯', color: '#20a338', className: 'state-green' },
  blue: { label: '蓝灯', color: '#1684ff', className: 'state-blue' },
  off: { label: '灭灯', color: '#9ca3af', className: 'state-off' }
}

const STATE_CODE_KEY = {
  0: 'off',
  1: 'red',
  2: 'yellow',
  3: 'green',
  4: 'blue',
  5: 'off'
}

export default {
  name: 'tricolorlamp',
  data() {
    return {
      activeTab: 'realtime',
      tabs: [
        { key: 'realtime', label: '实时状态' },
        { key: 'timeline', label: '时序状态' },
        { key: 'moving', label: '稼动率' },
        { key: 'startup', label: '开机率' },
        { key: 'exception', label: '异常统计' },
        { key: 'shift', label: '班次设置' },
        { key: 'plan', label: '计划停机' },
        { key: 'reason', label: '原因设置' },
        { key: 'install', label: '安灯设置' },
        { key: 'contacts', label: '告警联系人' }
      ],
      query: {
        projectName: '',
        groupName: '',
        dtuSn: '',
        date: ''
      },
      viewMode: 'list',
      devices: [],
      stateMap: {},
      rateMap: {},
      stateCountMap: {},
      counterMap: {},
      groupOptions: [],
      stateFilter: 'all',
      loading: false,
      fetching: false,
      autoRefresh: true,
      refreshInterval: 15000,
      refreshTimer: null,
      clockTimer: null,
      nowTick: Date.now(),
      lastRefreshTime: null,
      detailVisible: false,
      detailTitle: '',
      detailRows: [],
      selectedOeeCard: null,
      oeeDate: '',
      oeeQueryType: 'day',
      oeeLoading: false,
      oeeRows: [],
      oeeReasonFilter: '',
      durationChart: null,
      reasonChart: null,
      lightStack: [
        { key: 'red' },
        { key: 'yellow' },
        { key: 'green' },
        { key: 'blue' },
        { key: 'off' }
      ]
    }
  },
  computed: {
    lampCards() {
      return this.devices.map(device => {
        const dtuSn = this.pick(device, 'dtuSn', 'DtuSn')
        const currentState = this.stateMap[dtuSn] || {}
        const rateInfo = this.rateMap[dtuSn] || {}
        const stateKey = STATE_CODE_KEY[this.pick(currentState, 'lampState', 'LampState')] || 'off'
        const meta = STATE_META[stateKey]
        const durations = this.getDurations(rateInfo)
        const totalDuration = Object.keys(durations)
          .filter(key => key !== 'off')
          .reduce((sum, key) => sum + durations[key], 0)
        const greenDuration = durations.green || 0
        const startTime = this.pick(currentState, 'startTime', 'StartTime')
        const currentDuration = this.getCurrentLightDuration(startTime, stateKey, durations)
        const rate = totalDuration > 0 ? greenDuration / totalDuration * 100 : 0
        const counter = this.counterMap[dtuSn] || {}

        return {
          ...device,
          dtuSn,
          deviceName: this.pick(currentState, 'deviceName', 'DeviceName') || this.pick(rateInfo, 'deviceName', 'DeviceName') || this.pick(device, 'deviceName', 'DeviceName'),
          stateKey,
          stateName: meta.label,
          stateClass: meta.className,
          startTime,
          durations,
          rateText: `${rate.toFixed(2)}%`,
          currentDurationLabel: meta.label,
          currentDurationText: this.formatDuration(currentDuration),
          counterText: this.formatCounter(counter),
          counter,
          stateCount: this.stateCountMap[dtuSn] || {}
        }
      })
    },
    filteredCards() {
      const projectName = this.query.projectName.trim().toLowerCase()
      const dtuSn = this.query.dtuSn.trim().toLowerCase()
      return this.lampCards.filter(item => {
        const matchProject = !projectName || (item.deviceName || '').toLowerCase().includes(projectName)
        const matchSn = !dtuSn || (item.dtuSn || '').toLowerCase().includes(dtuSn)
        const matchState = this.stateFilter === 'all' || item.stateKey === this.stateFilter
        return matchProject && matchSn && matchState
      })
    },
    legendItems() {
      const counts = {
        all: this.lampCards.length,
        red: 0,
        yellow: 0,
        green: 0,
        blue: 0,
        off: 0
      }
      this.lampCards.forEach(item => {
        if (counts[item.stateKey] !== undefined) counts[item.stateKey]++
      })
      return ['all', 'red', 'yellow', 'green', 'blue', 'off'].map(key => ({
        key,
        label: STATE_META[key].label,
        color: STATE_META[key].color,
        count: counts[key]
      }))
    },
    lastRefreshText() {
      return this.lastRefreshTime || '未刷新'
    },
    oeeDeviceName() {
      if (!this.selectedOeeCard) return '-'
      return this.selectedOeeCard.deviceName || this.selectedOeeCard.dtuSn
    },
    oeeTimelineSegments() {
      return this.oeeRows.map(row => {
        const left = this.getDayPercent(row.startTime)
        const right = this.getDayPercent(row.endTime)
        return {
          ...row,
          left,
          width: Math.max(0.08, right - left)
        }
      })
    },
    oeeDurationSummary() {
      const summary = {
        red: 0,
        yellow: 0,
        green: 0,
        blue: 0,
        off: 0
      }
      this.oeeRows.forEach(row => {
        if (summary[row.stateKey] !== undefined) summary[row.stateKey] += row.duration
      })
      return summary
    }
  },
  created() {
    this.query.date = this.formatDate(new Date())
    this.loadLampData()
    this.startAutoRefresh()
    this.startClock()
  },
  beforeDestroy() {
    this.stopAutoRefresh()
    this.stopClock()
    this.disposeOeeCharts()
  },
  methods: {
    async loadLampData(options = {}) {
      if (this.fetching) return
      const silent = options.silent === true
      this.fetching = true
      this.loading = !silent
      try {
        const snapshotRes = await getRealtimeSnapshot(this.query.date)
        const snapshot = snapshotRes.data || {}
        let devices = this.pick(snapshot, 'devices', 'Devices') || []
        this.groupOptions = this.buildGroupOptions(devices)

        if (this.query.groupName) {
          const groupRes = await getUserGroupDtuSns(this.query.groupName)
          const groupDtuSns = new Set((groupRes.data || []).map(item => this.pick(item, 'dtuSn', 'DtuSn')))
          devices = devices.filter(item => groupDtuSns.has(this.pick(item, 'dtuSn', 'DtuSn')))
        }

        this.devices = devices
        this.stateMap = this.buildStateMap(this.pick(snapshot, 'states', 'States') || [])
        this.rateMap = this.buildRateMap(this.pick(snapshot, 'rates', 'Rates') || [])
        this.stateCountMap = this.buildSimpleMap(this.pick(snapshot, 'stateCounts', 'StateCounts') || [])
        this.counterMap = this.buildSimpleMap(this.pick(snapshot, 'counters', 'Counters') || [])
        this.lastRefreshTime = this.formatClock(new Date())
      } finally {
        this.fetching = false
        this.loading = false
      }
    },
    resetQuery() {
      this.query.projectName = ''
      this.query.dtuSn = ''
      this.query.date = this.formatDate(new Date())
      this.stateFilter = 'all'
      this.loadLampData()
    },
    startAutoRefresh() {
      this.stopAutoRefresh()
      if (!this.autoRefresh) return
      this.refreshTimer = window.setInterval(() => {
        this.loadLampData({ silent: true })
      }, this.refreshInterval)
    },
    stopAutoRefresh() {
      if (this.refreshTimer) {
        window.clearInterval(this.refreshTimer)
        this.refreshTimer = null
      }
    },
    restartAutoRefresh() {
      if (this.autoRefresh) this.startAutoRefresh()
    },
    handleAutoRefreshChange(value) {
      if (value) {
        this.startAutoRefresh()
      } else {
        this.stopAutoRefresh()
      }
    },
    startClock() {
      this.stopClock()
      this.clockTimer = window.setInterval(() => {
        this.nowTick = Date.now()
      }, 1000)
    },
    stopClock() {
      if (this.clockTimer) {
        window.clearInterval(this.clockTimer)
        this.clockTimer = null
      }
    },
    buildGroupOptions(list) {
      const values = list
        .map(item => this.pick(item, 'projectType', 'ProjectType'))
        .filter(item => item !== undefined && item !== null && item !== '')
      return Array.from(new Set(values))
    },
    buildStateMap(data) {
      const map = {}
      data.forEach(group => {
        const list = Array.isArray(group) ? group : [group]
        list.forEach(item => {
          const dtuSn = this.pick(item, 'dtuSn', 'DtuSn')
          if (item && dtuSn) map[dtuSn] = item
        })
      })
      return map
    },
    buildRateMap(data) {
      const map = {}
      data.forEach(day => {
        const rates = this.pick(day, 'realRate', 'RealRate') || []
        rates.forEach(item => {
          const dtuSn = this.pick(item, 'dtuSn', 'DtuSn')
          if (item && dtuSn) map[dtuSn] = item
        })
      })
      return map
    },
    buildSimpleMap(data) {
      const map = {}
      data.forEach(item => {
        const dtuSn = this.pick(item, 'dtuSn', 'DtuSn')
        if (item && dtuSn) map[dtuSn] = item
      })
      return map
    },
    getDurations(rateInfo) {
      return {
        off: this.toNumber(rateInfo['0'] || this.pick(rateInfo, 'state0', 'State0')),
        red: this.toNumber(rateInfo['1'] || this.pick(rateInfo, 'state1', 'State1')),
        yellow: this.toNumber(rateInfo['2'] || this.pick(rateInfo, 'state2', 'State2')),
        green: this.toNumber(rateInfo['3'] || this.pick(rateInfo, 'state3', 'State3')),
        blue: this.toNumber(rateInfo['4'] || this.pick(rateInfo, 'state4', 'State4')),
        other: this.toNumber(rateInfo['5'] || this.pick(rateInfo, 'state5', 'State5'))
      }
    },
    pick(source, ...keys) {
      if (!source) return undefined
      for (const key of keys) {
        if (source[key] !== undefined && source[key] !== null) return source[key]
      }
      return undefined
    },
    toNumber(value) {
      const number = Number(value)
      return Number.isNaN(number) ? 0 : number
    },
    getCurrentLightDuration(startTime, stateKey, durations) {
      if (startTime) {
        const start = new Date(String(startTime).replace(' ', 'T')).getTime()
        if (!Number.isNaN(start) && start > 0) {
          return Math.max(0, Math.floor((this.nowTick - start) / 1000))
        }
      }
      return durations[stateKey] || 0
    },
    formatDuration(seconds) {
      const value = Math.max(0, Math.floor(this.toNumber(seconds)))
      const hours = Math.floor(value / 3600)
      const minutes = Math.floor((value % 3600) / 60)
      const secs = value % 60
      if (hours > 0) return `${hours}时${minutes}分${secs}秒`
      if (minutes > 0) return `${minutes}分${secs}秒`
      return `${secs}秒`
    },
    formatCounter(counter) {
      const value = this.pick(counter, 'JS', 'Js', 'js') || 0
      return String(value).padStart(5, '0').slice(-5)
    },
    formatDate(date) {
      const y = date.getFullYear()
      const m = String(date.getMonth() + 1).padStart(2, '0')
      const d = String(date.getDate()).padStart(2, '0')
      return `${y}-${m}-${d}`
    },
    formatClock(date) {
      const h = String(date.getHours()).padStart(2, '0')
      const m = String(date.getMinutes()).padStart(2, '0')
      const s = String(date.getSeconds()).padStart(2, '0')
      return `${h}:${m}:${s}`
    },
    formatTime(value) {
      if (!value) return '暂无开始时间'
      if (value instanceof Date) {
        const y = value.getFullYear()
        const m = String(value.getMonth() + 1).padStart(2, '0')
        const d = String(value.getDate()).padStart(2, '0')
        const h = String(value.getHours()).padStart(2, '0')
        const min = String(value.getMinutes()).padStart(2, '0')
        const s = String(value.getSeconds()).padStart(2, '0')
        return `${y}-${m}-${d} ${h}:${min}:${s}`
      }
      return String(value).replace('T', ' ').slice(0, 19)
    },
    parseDate(value) {
      if (!value) return null
      if (value instanceof Date) return value
      const date = new Date(String(value).replace(' ', 'T'))
      return Number.isNaN(date.getTime()) ? null : date
    },
    getDayPercent(value) {
      const date = value instanceof Date ? value : this.parseDate(value)
      if (!date || !this.oeeDate) return 0
      const start = new Date(`${this.oeeDate}T00:00:00`)
      const end = new Date(start.getTime() + 24 * 60 * 60 * 1000)
      const clamped = Math.min(Math.max(date.getTime(), start.getTime()), end.getTime())
      return (clamped - start.getTime()) / (end.getTime() - start.getTime()) * 100
    },
    getTowerStyle(card, light) {
      const meta = STATE_META[light.key]
      if (card.stateKey === light.key) {
        return {
          background: meta.color,
          boxShadow: `0 0 12px ${meta.color}`
        }
      }
      return {}
    },
    async openOeeTimeline(card) {
      this.selectedOeeCard = card
      this.oeeDate = this.query.date || this.formatDate(new Date())
      this.viewMode = 'oee'
      this.stopAutoRefresh()
      await this.loadOeeData()
    },
    backToList() {
      this.viewMode = 'list'
      this.disposeOeeCharts()
      if (this.autoRefresh) this.startAutoRefresh()
    },
    async loadOeeData() {
      if (!this.selectedOeeCard || !this.oeeDate) return
      this.oeeLoading = true
      try {
        const res = await getDtuSnData(this.selectedOeeCard.dtuSn, this.oeeDate)
        const list = res.data || []
        const current = list[0] || {}
        const lampData = this.pick(current, 'lampData', 'LampData') || []
        this.oeeRows = lampData
          .map(item => this.normalizeOeeRow(item))
          .filter(Boolean)
          .sort((a, b) => a.startTime - b.startTime)
        this.$nextTick(() => this.renderOeeCharts())
      } finally {
        this.oeeLoading = false
      }
    },
    normalizeOeeRow(item) {
      const lampState = this.pick(item, 'lampState', 'LampState')
      const stateKey = STATE_CODE_KEY[lampState] || 'off'
      const meta = STATE_META[stateKey]
      const startTime = this.parseDate(this.pick(item, 'startTime', 'StartTime'))
      const apiEndTime = this.parseDate(this.pick(item, 'endTime', 'EndTime'))
      const duration = this.toNumber(this.pick(item, 'duration', 'Duration'))
      if (!startTime) return null
      const endTime = apiEndTime || new Date(startTime.getTime() + duration * 1000)
      const safeDuration = duration > 0 ? duration : Math.max(0, Math.floor((endTime - startTime) / 1000))
      return {
        startTime,
        endTime,
        startTimeText: this.formatTime(startTime),
        stateKey,
        stateName: meta.label,
        color: meta.color,
        duration: safeDuration,
        durationText: this.formatDuration(safeDuration),
        reason: '无',
        operator: '设备上传'
      }
    },
    renderOeeCharts() {
      if (!this.$refs.durationChart || !this.$refs.reasonChart) return
      this.durationChart = this.durationChart || echarts.init(this.$refs.durationChart)
      this.reasonChart = this.reasonChart || echarts.init(this.$refs.reasonChart)

      const durationData = ['green', 'red', 'yellow', 'blue', 'off']
        .map(key => ({
          name: STATE_META[key].label,
          value: this.oeeDurationSummary[key],
          itemStyle: { normal: { color: STATE_META[key].color }}
        }))
        .filter(item => item.value > 0)

      this.durationChart.setOption({
        color: durationData.map(item => item.itemStyle.normal.color),
        tooltip: {
          trigger: 'item',
          formatter: params => `${params.name}<br/>${this.formatDuration(params.value)} (${params.percent}%)`
        },
        legend: {
          top: 16,
          left: 'center',
          itemWidth: 10,
          itemHeight: 10,
          textStyle: { color: '#5b6673', fontSize: 12 }
        },
        series: [{
          type: 'pie',
          radius: ['0%', '58%'],
          center: ['50%', '58%'],
          minAngle: 2,
          label: {
            color: '#fff',
            formatter: params => `${params.percent}%\n${params.name}:${this.formatDuration(params.value)}`
          },
          data: durationData.length ? durationData : [{ name: '暂无数据', value: 1, itemStyle: { normal: { color: '#d8dee8' }}}]
        }]
      })

      const abnormal = [
        { name: '红灯总时长', value: this.oeeDurationSummary.red, color: STATE_META.red.color },
        { name: '黄灯总时长', value: this.oeeDurationSummary.yellow, color: STATE_META.yellow.color }
      ].filter(item => item.value > 0)
      const reasonData = abnormal.length ? abnormal : [{ name: '暂无异常', value: 1, color: '#d8dee8' }]

      this.reasonChart.setOption({
        color: reasonData.map(item => item.color),
        tooltip: {
          trigger: 'item',
          formatter: params => `${params.name}<br/>${this.formatDuration(params.value)}`
        },
        legend: {
          orient: 'vertical',
          right: 22,
          top: 42,
          itemWidth: 10,
          itemHeight: 10,
          textStyle: { color: '#5b6673', fontSize: 12 }
        },
        series: [{
          type: 'pie',
          radius: ['48%', '64%'],
          center: ['45%', '58%'],
          avoidLabelOverlap: true,
          label: { show: false },
          data: reasonData.map(item => ({
            name: item.name,
            value: item.value,
            itemStyle: { normal: { color: item.color }}
          }))
        }]
      })
    },
    disposeOeeCharts() {
      if (this.durationChart) {
        this.durationChart.dispose()
        this.durationChart = null
      }
      if (this.reasonChart) {
        this.reasonChart.dispose()
        this.reasonChart = null
      }
    },
    downloadOeeData() {
      this.$message.info('OEE时序导出功能待接入')
    },
    openDetail(card) {
      this.detailTitle = `${card.deviceName || card.dtuSn} 稼动率详情`
      this.detailRows = [
        { name: '稼动率', value: card.rateText },
        { name: '红灯时长', value: this.formatDuration(card.durations.red) },
        { name: '黄灯时长', value: this.formatDuration(card.durations.yellow) },
        { name: '绿灯时长', value: this.formatDuration(card.durations.green) },
        { name: '蓝灯时长', value: this.formatDuration(card.durations.blue) },
        { name: '灭灯时长', value: this.formatDuration(card.durations.off) }
      ]
      this.detailVisible = true
    },
    openCounter(card) {
      this.detailTitle = `${card.deviceName || card.dtuSn} 计数明细`
      this.detailRows = [
        { name: '计数', value: this.pick(card.counter, 'JS', 'Js', 'js') || 0 },
        { name: '总时间', value: this.pick(card.counter, 'ZSJ', 'Zsj', 'zsj') || '-' },
        { name: '红灯时间', value: this.pick(card.counter, 'HSJ', 'Hsj', 'hsj') || '-' },
        { name: '绿灯时间', value: this.pick(card.counter, 'FSJ', 'Fsj', 'fsj') || '-' },
        { name: '更新时间', value: this.pick(card.counter, 'modifyTime', 'ModifyTime') || '-' }
      ]
      this.detailVisible = true
    },
    openSetting(card) {
      this.$message.info(`${card.deviceName || card.dtuSn} 设置功能待配置`)
    }
  }
}
</script>

<style scoped>
.smart-lamp-page {
  min-height: calc(100vh - 84px);
  background: #f6f8fb;
  color: #1f2d3d;
}

.lamp-tabs {
  display: flex;
  align-items: center;
  height: 34px;
  border-bottom: 1px solid #dfe7f0;
  background: #fff;
  overflow-x: auto;
  white-space: nowrap;
}

.lamp-tab {
  display: inline-flex;
  align-items: center;
  height: 34px;
  padding: 0 14px;
  font-size: 12px;
  color: #3c4a57;
  cursor: pointer;
  border-right: 1px solid #edf2f7;
}

.lamp-tab.active {
  color: #1684ff;
  font-weight: 600;
  background: #f8fbff;
}

.lamp-toolbar {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 48px;
  padding: 7px 18px;
  background: #fff;
  border-bottom: 1px solid #e8edf4;
}

.project-input {
  width: 178px;
}

.group-select {
  width: 170px;
}

.sn-input {
  width: 150px;
}

.date-input {
  width: 140px;
}

.refresh-select {
  width: 84px;
}

.toolbar-spacer {
  flex: 1;
}

.refresh-tip {
  font-size: 12px;
  color: #6b7785;
}

.status-row {
  display: flex;
  align-items: center;
  gap: 28px;
  min-height: 44px;
  padding: 8px 28px;
  flex-wrap: wrap;
  background: #fff;
}

.legend-item {
  display: inline-flex;
  align-items: center;
  font-size: 12px;
  color: #303133;
  cursor: pointer;
  padding: 5px 8px;
  border-radius: 4px;
}

.legend-item.active {
  background: #edf5ff;
  font-weight: 700;
}

.legend-color {
  width: 22px;
  height: 11px;
  border-radius: 2px;
  margin-right: 5px;
}

.lamp-board {
  display: flex;
  align-items: flex-start;
  gap: 18px;
  flex-wrap: wrap;
  min-height: 420px;
  padding: 16px 18px 30px;
}

.lamp-card {
  position: relative;
  width: 178px;
  min-height: 246px;
  border-radius: 8px;
  background: linear-gradient(145deg, #0d8e26 0%, #7ac665 100%);
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.18);
  color: #fff;
  overflow: hidden;
}

.lamp-card.state-red {
  background: linear-gradient(145deg, #b51220 0%, #f17983 100%);
}

.lamp-card.state-yellow {
  background: linear-gradient(145deg, #d98600 0%, #f5c04f 100%);
}

.lamp-card.state-blue {
  background: linear-gradient(145deg, #0065b5 0%, #66a9dc 100%);
}

.lamp-card.state-off {
  background: linear-gradient(145deg, #4b5563 0%, #9ca3af 100%);
}

.card-glow {
  position: absolute;
  top: -32px;
  right: -42px;
  width: 96px;
  height: 96px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.24);
}

.card-title {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 31px;
  padding: 0 9px;
  font-size: 13px;
  font-weight: 700;
}

.device-name {
  max-width: 105px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.state-pill {
  padding: 2px 6px;
  border-radius: 999px;
  font-size: 11px;
  background: rgba(255, 255, 255, 0.22);
}

.card-body {
  position: relative;
  display: flex;
  padding: 8px 12px 0 20px;
}

.tower {
  width: 22px;
  height: 122px;
  border-radius: 4px;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.56);
  display: flex;
  flex-direction: column;
  justify-content: space-around;
  padding: 5px 0;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.42);
}

.tower-light {
  width: 22px;
  height: 18px;
  background: rgba(53, 65, 79, 0.68);
  opacity: 0.76;
}

.tower-light.on {
  opacity: 1;
}

.card-main {
  flex: 1;
  min-width: 0;
  padding-left: 12px;
  padding-top: 14px;
}

.metric-label,
.current-time {
  font-size: 12px;
  font-weight: 700;
  line-height: 18px;
}

.metric-value {
  font-size: 18px;
  line-height: 22px;
  font-weight: 800;
}

.counter-box {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 100px;
  height: 33px;
  margin-top: 14px;
  border-radius: 5px;
  background: #f5fff0;
  color: #101820;
  font-family: Consolas, Monaco, monospace;
  font-size: 25px;
  letter-spacing: 3px;
  box-shadow: inset 0 0 0 1px rgba(15, 23, 42, 0.12);
}

.card-meta {
  position: relative;
  display: grid;
  grid-template-columns: 1fr;
  gap: 2px;
  padding: 8px 10px 0;
  font-size: 12px;
  color: rgba(255, 255, 255, 0.9);
}

.card-actions {
  position: relative;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px 10px;
  padding: 14px 8px 12px;
}

.card-actions .el-button {
  margin: 0;
  padding: 5px 7px;
  border: 0;
  color: #fff;
  border-radius: 4px;
  font-size: 11px;
}

.state-green .card-actions .el-button {
  background: rgba(0, 137, 70, 0.9);
}

.state-green .card-actions .el-button:hover {
  background: rgba(0, 115, 60, 0.98);
}

.state-yellow .card-actions .el-button {
  background: rgba(207, 124, 0, 0.9);
}

.state-yellow .card-actions .el-button:hover {
  background: rgba(178, 104, 0, 0.98);
}

.state-red .card-actions .el-button {
  background: rgba(184, 26, 42, 0.9);
}

.state-red .card-actions .el-button:hover {
  background: rgba(154, 20, 34, 0.98);
}

.state-blue .card-actions .el-button {
  background: rgba(0, 105, 180, 0.9);
}

.state-blue .card-actions .el-button:hover {
  background: rgba(0, 86, 148, 0.98);
}

.state-off .card-actions .el-button {
  background: rgba(75, 85, 99, 0.9);
}

.state-off .card-actions .el-button:hover {
  background: rgba(55, 65, 81, 0.98);
}

.oee-page {
  min-height: calc(100vh - 118px);
  background: #f2f4f7;
}

.oee-topbar {
  display: flex;
  align-items: center;
  gap: 8px;
  height: 36px;
  padding: 0 10px;
  background: #fff;
  border-bottom: 1px solid #cfd6df;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.12);
  font-size: 12px;
}

.oee-breadcrumb {
  color: #293241;
  font-weight: 600;
}

.oee-breadcrumb.active {
  color: #1684ff;
  cursor: pointer;
}

.oee-label {
  color: #4b5563;
}

.oee-date {
  width: 160px;
}

.oee-spacer {
  flex: 1;
}

.oee-timeline-panel,
.oee-detail-card,
.oee-chart-card {
  background: #fff;
  border: 1px solid #d9e0e8;
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.14);
}

.oee-timeline-panel {
  margin: 8px 8px 6px;
  padding: 22px 28px 20px;
  height: 154px;
}

.oee-panel-title {
  color: #303133;
  font-size: 13px;
  font-weight: 700;
}

.timeline-device {
  margin: 10px 0 16px 28px;
  color: #657180;
  font-size: 12px;
}

.day-track {
  position: relative;
  height: 38px;
  margin: 0 38px;
  background: #e5e7eb;
  overflow: hidden;
}

.day-segment {
  position: absolute;
  top: 0;
  bottom: 0;
  min-width: 2px;
}

.day-scale {
  display: flex;
  justify-content: space-between;
  margin: 8px 34px 0;
  color: #697586;
  font-size: 11px;
}

.oee-content {
  display: grid;
  grid-template-columns: minmax(460px, 1.1fr) minmax(260px, 0.8fr) minmax(260px, 0.8fr);
  gap: 6px;
  padding: 0 8px 12px;
}

.oee-detail-card,
.oee-chart-card {
  min-height: 378px;
}

.oee-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 44px;
  padding: 0 14px;
  color: #303133;
  font-size: 13px;
  font-weight: 700;
}

.reason-select {
  width: 138px;
  margin-left: 8px;
}

.oee-table {
  padding: 0 14px 12px;
}

.state-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  margin-right: 5px;
  border-radius: 50%;
  vertical-align: middle;
}

.chart-title {
  height: 48px;
  line-height: 48px;
  padding-left: 34px;
  color: #303133;
  font-size: 13px;
  font-weight: 700;
}

.chart-box {
  height: 310px;
}

@media (max-width: 980px) {
  .lamp-toolbar {
    height: auto;
    flex-wrap: wrap;
    padding: 8px 12px;
  }

  .toolbar-spacer {
    display: none;
  }

  .status-row {
    padding: 8px 12px;
    gap: 12px;
  }

  .lamp-board {
    padding: 12px;
  }

  .oee-content {
    grid-template-columns: 1fr;
  }

  .oee-topbar {
    height: auto;
    min-height: 36px;
    flex-wrap: wrap;
    padding: 6px 10px;
  }
}
</style>
