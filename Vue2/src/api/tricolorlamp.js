import request from '@/utils/request'

export function getUserDtuSns() {
  return request({
    url: '/TriColorLamp/GetUserDtuSns',
    method: 'get'
  })
}

export function getRealtimeSnapshot(date) {
  return request({
    url: '/TriColorLamp/GetRealtimeSnapshot',
    method: 'get',
    params: { date }
  })
}

export function getUserGroupDtuSns(groupName) {
  return request({
    url: '/TriColorLamp/GetUserGroupDtuSns',
    method: 'get',
    params: { groupName }
  })
}

export function getDtuSnData(dtuSn, date) {
  return request({
    url: '/TriColorLamp/GetDtuSnData',
    method: 'get',
    params: { dtuSn, date }
  })
}

export function getDtuSnStateList(dtuSns) {
  return request({
    url: '/TriColorLamp/GetDtuSnStateList',
    method: 'get',
    params: { dtuSns }
  })
}

export function getDtuSnListRateOfAction(date, dtuSns) {
  return request({
    url: '/TriColorLamp/GetDtuSnListRateOfAction',
    method: 'get',
    params: { date, dtuSns }
  })
}

export function getDtuSnListStateCount(dtuSns) {
  return request({
    url: '/TriColorLamp/GetDtuSnListStateCount',
    method: 'get',
    params: { dtuSns }
  })
}

export function getDtuSnListCounter(dtuSns) {
  return request({
    url: '/TriColorLamp/GetDtuSnListCounter',
    method: 'get',
    params: { dtuSns }
  })
}
