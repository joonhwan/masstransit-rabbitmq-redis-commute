using System;
using System.Collections.Generic;
using System.Linq;
using HelloQuartzWorker.Job;
using HelloQuartzWorker.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace HelloQuartzWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IJobFactory, JobFactory>();
                    services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
                    services.AddSingleton<NotificationJob>();

                    services.AddSingleton(
                        new JobMetaData(
                            Guid.NewGuid(),
                            typeof(NotificationJob),
                            "Notify Job",
                            "0/10 * * * * ?"
                        )
                    );
                    
                    // services.AddHostedService<Worker>();
                    services.AddHostedService<MyService>();
                });
    }
}