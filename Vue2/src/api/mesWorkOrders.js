import request from '@/utils/request'
/**
 * 分页查询派工单
 */
export function getMesWorkOrderPage(data) {
  return request({
    url: '/mesworkorder/getpage',
    method: 'post',
    data,
  })
}

/**
 * 获取全部派工单（自动翻页）
 */
export function getMesWorkOrderAll(data) {
  return request({
    url: '/mesworkorder/getall',
    method: 'post',
    data,
  })
}

/**
 * 校验派工单
 */
export function checkMesWorkOrder(workOrder) {
  return request({
    url: '/mesworkorder/checkworkorder',
    method: 'get',
    params: { workOrder },
  })
}

/**
 * 获取派工单详情
 */
export function getMesWorkOrderDetail(workOrder) {
  return request({
    url: '/mesworkorder/getworkorderdetail',
    method: 'get',
    params: { workOrder },
  })
}