const path = require('path')
function resolve (dir) {
    return path.join(__dirname, '/', dir)
}

module.exports = {
  runtimeCompiler:true,
  configureWebpack: {
    devtool: process.env.NODE_ENV === 'production' ? false : 'eval-cheap-module-source-map',
    optimization: process.env.NODE_ENV === 'production' ? {} : {
      removeAvailableModules: false,
      removeEmptyChunks: false,
      splitChunks: false
    }
  },
  productionSourceMap: false,
  lintOnSave: false,
  devServer: {
    port: 1803,     // 端口
    disableHostCheck: true,
    overlay: {
      warnings: true,
      errors: false
    }
  }
}
