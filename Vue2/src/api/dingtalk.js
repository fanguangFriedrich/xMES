import request from '@/utils/request'

/**
 * 钉钉端内免密登录（通过免登授权码）
 * @param {string} code 钉钉免登授权码
 */
export function loginByDingTalkApp(code) {
  return request({
    url: '/DingTalkLogin/LoginByDingTalkApp',
    method: 'get',
    params: { code }
  })
}

/**
 * 网页扫码登录（通过 authCode）
 * @param {string} authCode 钉钉扫码授权码
 */
export function loginByScanCode(authCode) {
  return request({
    url: '/DingTalkLogin/LoginByScanCode',
    method: 'get',
    params: { authCode }
  })
}

// ─── 部门相关 ────────────────────────────────────────────

/**
 * 获取指定父部门下的子部门ID列表
 * @param {number} deptId 父部门ID，根部门传1
 */
export function getSubDeptIdList(deptId) {
  return request({
    url: '/DingTalk/GetSubDeptIdList',
    method: 'get',
    params: { deptId }
  })
}

/**
 * 获取企业所有部门ID列表（从根部门递归遍历）
 */
export function getAllDeptIdList() {
  return request({
    url: '/DingTalk/GetAllDeptIdList',
    method: 'get'
  })
}

/**
 * 获取指定父部门下的子部门列表（含部门详情）
 * @param {number} [deptId] 父部门ID，不传默认从根部门开始
 */
export function getSubDeptList(deptId) {
  return request({
    url: '/DingTalk/GetSubDeptList',
    method: 'get',
    params: deptId !== undefined ? { deptId } : {}
  })
}

/**
 * 递归获取企业所有部门列表（含详情）
 */
export function getAllDeptList() {
  return request({
    url: '/DingTalk/GetAllDeptList',
    method: 'get'
  })
}

// ─── 员工相关 ────────────────────────────────────────────
/**
 * 通过 userId 获取用户详细信息
 * @param {string} userId 钉钉用户userId
 */
export function getUserByUserId(userId) {
  return request({
    url: '/DingTalk/GetUserByUserId',
    method: 'get',
    params: { userId }
  })
}

/**
 * 获取企业所有员工列表（遍历全部部门，自动去重）
 */
export function getAllUsers() {
  return request({
    url: '/DingTalk/GetAllUsers',
    method: 'get'
  })
}

/**
 * 获取指定部门的员工列表（自动翻页，返回全量）
 * @param {number} deptId 部门ID
 */
export function getDeptUserList(deptId) {
  return request({
    url: '/DingTalk/GetDeptUserList',
    method: 'get',
    params: { deptId }
  })
}



/**
 * 获取企业 AccessToken（调试用）
 * @param {string} corpId 企业ID
 */
export function getCorpAccessToken(corpId) {
  return request({
    url: '/DingTalk/GetCorpAccessToken',
    method: 'get',
    params: { corpId }
  })
}

/**
 * 通过部门ID获取部门详细信息
 * @param {number} deptId 部门ID
 */
export function getDeptById(deptId) {
  return request({
    url: '/DingTalk/GetDeptById',
    method: 'get',
    params: { deptId }
  })
}

/**
 * 获取钉钉配置信息（clientId、corpId）
 */
export function getDingTalkOptions() {
  return request({
    url: '/DingTalk/GetDingTalkOptions',
    method: 'get'
  })
}

/**
 * 触发异步同步部门任务，立即返回 taskId
 */
export function startSyncAllDeptList() {
  return request({
    url: '/DingTalk/StartSyncAllDeptList',
    method: 'post'
  })
}

/**
 * 轮询同步任务状态
 * @param {string} taskId 任务ID
 */
export function getSyncDeptStatus(taskId) {
  return request({
    url: '/DingTalk/GetSyncDeptStatus',
    method: 'get',
    params: { taskId }
  })
}

/**
 * 发送钉钉文本工作通知
 * @param {object} data 文本工作通知参数
 */
export function sendTextWorkNotification(data) {
  return request({
    url: '/DingTalk/SendTextWorkNotification',
    method: 'post',
    data
  })
}
