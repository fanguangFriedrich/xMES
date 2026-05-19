using Autofac.Extensions.DependencyInjection;
using Infrastructure;
using Infrastructure.Helpers;
using Jusoft.DingtalkStream.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAuth.App.DingTalk;
using StackExchange.Redis;
using System;

namespace OpenAuth.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($@"
               ____                                 _   _       _   _      _   
              / __ \                     /\        | | | |     | \ | |    | |  
             | |  | |_ __   ___ _ __    /  \  _   _| |_| |__   |  \| | ___| |_ 
             | |  | | '_ \ / _ \ '_ \  / /\ \| | | | __| '_ \  | . ` |/ _ \ __|
             | |__| | |_) |  __/ | | |/ ____ \ |_| | |_| | | |_| |\  |  __/ |_ 
              \____/| .__/ \___|_| |_/_/    \_\__,_|\__|_| |_(_)_| \_|\___|\__|
                    | |                                                        
                    |_|                                                        
            -------------------------------------------------------------------
            Author           :  Fred Xue
            -------------------------------------------------------------------
            Start Time:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders(); //去掉默认的日志
                    // logging.AddLog4Net();
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory()) //将默认ServiceProviderFactory指定为AutofacServiceProviderFactory
                .ConfigureServices(services =>
                {
                    // 从配置文件读取钉钉参数
                    var configuration = ConfigHelper.GetConfigRoot();
                    var clientId = configuration["DingTalk:ClientId"];
                    var clientSecret = configuration["DingTalk:ClientSecret"];

                    services.AddDingtalkStream(options =>
                    {
                        options.ClientId     = clientId;
                        options.ClientSecret = clientSecret;
                        options.AutoReplySystemMessage = true;
                    })
                    .RegisterEventSubscription()
                    .AddMessageHandler<DingTalkStreamMessageHandler>()
                    .AddHostServices();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var configuration = ConfigHelper.GetConfigRoot();
                    var httpHost = configuration["AppSetting:HttpHost"];
                    
                    webBuilder.UseUrls(httpHost).UseStartup<Startup>();
                    Console.WriteLine($"启动成功，接口访问地址:{httpHost}/swagger/index.html");
                });
    }
}