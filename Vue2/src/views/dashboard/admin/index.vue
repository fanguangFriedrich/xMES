<!--
 * @Author: yubaolee <yubaolee@163.com> | ahfu~ <954478625@qq.com>
 * @Date: 2021-06-01 14:10:29
 * @LastEditTime: 2025-06-08 14:07:28
 * @Description: 
 * @
 * @Copyright (c) 2022 by yubaolee | ahfu~ , All Rights Reserved. 
-->
<template>
  <div class="dashboard-editor-container">

    
  </div>
</template>


<script>

  export default {
    name: 'dashboard-admin',
    data() {
      return {
        auths: [
      {
        name: '价格',
        belongs: ['opensource', 'annual', 'lifetime'],
        opensourceRemark: '永久免费',
        annualRemark: '1688元/年',
        lifetimeRemark: '3000元/永久',
      },
      {
        name: '代码获取',
        belongs: ['opensource', 'annual', 'lifetime'],
        opensourceRemark: '开源中国、GitHub',
        annualRemark: '平台下载、持续更新',
        lifetimeRemark: '平台下载、持续更新',
      },
      {
        name: '授权域名个数',
        belongs: ['opensource', 'annual', 'lifetime'],
        opensourceRemark: '无限制',
        annualRemark: '无限制',
        lifetimeRemark: '无限制',
      },
      {
        name: '授权项目个数',
        belongs: ['opensource', 'annual', 'lifetime'],
        opensourceRemark: '无限制',
        annualRemark: '无限制',
        lifetimeRemark: '无限制',
      },
      {
        name: '永久使用',
        belongs: ['opensource', 'annual', 'lifetime'],
      },
      {
        name: 'Vue2+ElementUI源码',
        belongs: ['opensource', 'annual', 'lifetime'],
      },
      {
        name: '专属QQ群',
        belongs: ['annual', 'lifetime'],
        opensourceRemark: '可进大群',
      },
      {
        name: '提供正规发票',
        belongs: ['annual', 'lifetime'],
      },
      {
        name: 'Vue3+ElementPlus源码',
        belongs: ['annual', 'lifetime'],
        annualRemark: '订阅期内有效',
      },
      {
        name: '移动UniApp源码',
        belongs: ['annual', 'lifetime'],
        annualRemark: '订阅期内有效',
      },
      {
        name: '技术支持服务',
        belongs: ['annual', 'lifetime'],
        annualRemark: '订阅期内有效',
      }
    ],
        goods: [{
          name: '开源版',
          subTitle: '',
          type: 'opensource'
        },{
          name: '年费订阅',
          subTitle: '',
          type: 'annual'
        },{
          name: '终生订阅',
          subTitle: '',
          type: 'lifetime',
          recomment: true
        }]
      }
    },
  }

</script>

<style rel="stylesheet/scss" lang="scss" scoped>
  .auth-box{
    margin-top: 30px;
    background: #f5f5f5;
    .auth-col{
      border: 1px solid #c8c8c8;
      &.active{
        // background: #0dafff;
        overflow: hidden;
        position: relative;
        .recomment{
          display: inline-block;
          width: 150px;
          height: 35px;
          line-height: 35px;
          text-align: center;
          background: #febd17;
          transform: rotate(-45deg);
          position: absolute;
          left: -45px;
          top: 10px;
          font-size: 14px;
          color: #fff;
          font-weight: normal;
          letter-spacing: 3px;
        }
        .auth-head{

        }
      }
      &:nth-child(2){
        border-left: 0;
        border-right: 0;
      }
      &:nth-child(4){
        border-left: 0;
      }
      // border-right: 
      .auth-head{
        text-align: center;
        font-size: 18px;
        font-weight: 600;
        height: 135px;
        .subTitle{
          display: inline-block;
          font-weight: normal;
          padding-top: 10px;
        }
      }
      >div{
        height: 40px;
        padding: 10px 20px;
        box-sizing: border-box;
        text-align: center;
        .auth-icon{
          font-size: 18px;
          padding: 0 10px;
          .el-icon-success{
            color: #67c23a;
          }
          .el-icon-error{
            color: #f56c6c;
          }
        }
      }
      > div:nth-child(even) {
        background: #e5e5e5;
      }
      .auth-item:nth-child(even) {
        // background: #0dafff;
      }
    }
  }
  .dashboard-editor-container {
    padding: 20px;
    background-color: rgb(240, 242, 245);
    min-height: calc(100vh - 84px);

    .chart-wrapper {
      background: #fff;
      padding: 16px 16px 0;
      margin-bottom: 32px;
    }

    .component-item {
      min-height: 100px;
    }
  }

  .product {
    max-width: 350px;
    border: 1px solid #dfdfdf;
    padding: 0;
    border-radius: 8px;
    margin-bottom: 20px;
    background-color: white;

    .title {
      padding: 12px;
      text-align: center;
      background-color: #f4f4f4;
      border-radius: 8px 8px 0 0;
    }

    h2 {
      margin: 24px 0 12px;
    }

    .content {
      h2 {
        text-align: center;
        font-size: 30px;
      }

      .tips {
        padding: 0 35px 30px 30px;

        div {
          padding: 8px 0;
          font-size: 15px;

          .el-icon-check {
            width: 30px;
            font-size: 1.5em;
            color: #35affb;
            vertical-align: middle;
          }
        }

        .color-prime {
          color: #338ed6;
          font-weight: 650;

          .el-icon-check {
            color: gray;
          }
        }

        .color-enterprise {
          color: #f56c6c;
          font-weight: 800;

          .el-icon-check {
            color: #67c23a;
          }
        }
      }
    }
  }

  .action {
    text-align: center;
    padding-bottom: 24px;
    padding: 12px 18px;
    display: flex;
    background: #f4f4f4;

    .btn {
      flex: 1;
      margin: 6px;
      padding: 6px 12px;
      line-height: 26px;
      font-size: 16px;
      background: #fff;
      position: relative;
      cursor: pointer;
      display: inline-block;
      white-space: nowrap;
      border-radius: 4px;
      -ms-touch-action: manipulation;
      touch-action: manipulation;
      font-weight: 700;
      text-decoration: none;
      border: 2px solid #4893ff;
      background-color: transparent;
      color: #4893ff;
      transition: all 0.3s;
    }

    a.btn.std {
      color: #0dafff;
      border-color: #0dafff;
    }

    a.btn.premier {
      color: #d67735;
      border-color: #ffa669;
    }
  }

  .standard .title {
    background: #eaeaea;
    color: #666;
  }

  .prime {
    margin: 0 auto;
  }

  .prime .title {
    background: linear-gradient(#338ed6, #4f6eab);
    color: #fff;
  }

  .enterprise {
    margin-left: auto;
    margin-right: 0;
  }

  .enterprise .title {
    background: linear-gradient(#807979, #484a4e);
    color: #fff;
  }

</style>
