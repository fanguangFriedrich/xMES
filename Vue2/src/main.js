import Vue from 'vue'

import 'normalize.css/normalize.css'

import ElementUI from 'element-ui'
import 'element-ui/lib/theme-chalk/index.css'
import '@/assets/custom-theme/index.css'
import locale from 'element-ui/lib/locale/lang/zh-CN'
import VueContextMenu from 'vue-contextmenu'

import '@/styles/index.scss'

import App from './App'
import router from './router'
import store from './store'

import '@/permission'

import '@/assets/public/css/comIconfont/iconfont.css'
import '@/assets/public/css/comIconfont/iconfont.js'

import '../public/ueditor/ueditor.config.js'
import '../public/ueditor/ueditor.all.js'
import '../public/ueditor/lang/zh-cn/zh-cn.js'
import '../public/ueditor/formdesign/leipi.formdesign.v4.js'

import vform from '@/lib/vform/VFormDesigner.umd.min.js'
import '@/lib/vform/VFormDesigner.css'

import moment from 'moment'
Date.prototype.toISOString = function(){
    return moment(this).format('YYYY-MM-DDTHH:mm:ssZZ')
}

import { disAutoConnect } from 'vue-plugin-hiprint'
disAutoConnect();

// 新增
import { getDingTalkOptions } from '@/api/dingtalk'

if (process.env.NODE_ENV === 'development') {
  new window.VConsole();
}

Vue.use(ElementUI, { locale })
Vue.use(VueContextMenu)
Vue.use(vform)

Vue.config.productionTip = false
Vue.config.devtools = true

// 新增：改为async bootstrap启动
async function bootstrap() {
  try {
    const res = await getDingTalkOptions()
    Vue.prototype.$dingTalk = {
      corpId: res.data?.corpId || '',
      clientId: res.data?.clientId || '',
    }
  } catch (e) {
    console.error('获取钉钉配置失败:', e)
    Vue.prototype.$dingTalk = { corpId: '', clientId: '' }
  }

  new Vue({
    el: '#app',
    router,
    store,
    render: h => h(App)
  })
}

bootstrap()