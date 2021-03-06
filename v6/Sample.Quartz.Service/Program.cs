﻿using GreenPipes;
using MassTransit;
using MassTransit.QuartzIntegration;
using MassTransit.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog;
using Serilog.Events;

namespace Sample.Quartz.Service
{
    class Program
    {
        static async Task Main(string[] args)
        {   
            const int eucKrCodePage = 51949; // euc-kr 코드 번호
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var eucKr = Encoding.GetEncoding(eucKrCodePage);
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("sample.quartz.service-.log", rollingInterval: RollingInterval.Day,  encoding: eucKr)
                .CreateLogger();

            
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = CreateHostBuilder(args);

            if (isService)
            {
                await builder.UseWindowsService().Build().RunAsync();
            }
            else
            {
                await builder.RunConsoleAsync();
            }
            
            Log.CloseAndFlush();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables();

                if (args != null)
                    config.AddCommandLine(args);
            })
            .ConfigureLogging((context, logging) =>
            {
                // logging.AddConsole(options =>
                // {
                //     options.TimestampFormat = "[HH:mm:ss] ";
                // });
                logging.AddSerilog(dispose: true);
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
                services.Configure<QuartzConfig>(hostContext.Configuration.GetSection("quartz"));

                // Service Bus
                services.AddMassTransit(configurator =>
                {
                    configurator.AddBus(provider =>
                    {
                        return Bus.Factory.CreateUsingRabbitMq(cfg =>  
                        {
                            var scheduler = provider.GetRequiredService<IScheduler>();
                            var options = provider.GetRequiredService<IOptions<AppConfig>>().Value;

                            cfg.Host(options.Host, options.VirtualHost, h =>
                            {
                                h.Username(options.Username);
                                h.Password(options.Password);
                            });

                            cfg.UseJsonSerializer(); // Because we are using json within Quartz for serializer type

                            cfg.ReceiveEndpoint(options.QueueName, endpoint =>
                            {
                                var partitionCount = Environment.ProcessorCount;
                                endpoint.PrefetchCount = (ushort)(partitionCount);
                                var partitioner = endpoint.CreatePartitioner(partitionCount);

                                endpoint.Consumer(() => new ScheduleMessageConsumer(scheduler), x =>
                                    x.Message<ScheduleMessage>(m => m.UsePartitioner(partitioner, p => p.Message.CorrelationId)));
                                endpoint.Consumer(() => new CancelScheduledMessageConsumer(scheduler),
                                    x => x.Message<CancelScheduledMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));
                            });
                        }); 
                    });
                    
                });

                services.AddHostedService<MassTransitConsoleHostedService>();

                services.AddSingleton(x =>
                {
                    var connectionString = hostContext.Configuration.GetConnectionString("scheduler-db");
                    DbInitializer.InitializeDb(connectionString);
                    var quartzConfig = x.GetRequiredService<IOptions<QuartzConfig>>().Value
                        .UpdateConnectionString(connectionString)
                        .ToNameValueCollection();
                    return new StdSchedulerFactory(quartzConfig).GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();

                });
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            });
    }
}