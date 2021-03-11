using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GreenPipes;
using LongRun.Components;
using LongRun.Contracts;
using MassTransit;
using MassTransit.Conductor;
using MassTransit.DapperIntegration;
using MassTransit.Definition;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.EntityFrameworkCoreIntegration.JobService;
using MassTransit.JobService;
using MassTransit.JobService.Components.StateMachines;
using MassTransit.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace LongRun.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                // .MinimumLevel.Debug()
                .MinimumLevel.Information()
                .MinimumLevel.Override("JobService", Serilog.Events.LogEventLevel.Debug)
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
            
            Log.CloseAndFlush();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                })
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    // "host=localhost;user id=postgres;password=Password12!;database=JobService;"
                    var connectionString = configuration.GetConnectionString("JobService");
                    Console.WriteLine("@Using Connection String : {0}", connectionString);

                    services.AddDbContext<JobServiceSagaDbContext>(builder =>
                        builder.UseNpgsql(connectionString,
                            m =>
                            {
                                m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                                m.MigrationsHistoryTable($"__{nameof(JobServiceSagaDbContext)}");
                            }));

                    services.AddMassTransit(c =>
                    {
                        c.SetKebabCaseEndpointNameFormatter(); //@global-name-formatter 

                        c.AddRabbitMqMessageScheduler();

                        //c.AddConsumersFromNamespaceContaining<DoItConsumer>();
                        //c.AddConsumer<DoItConsumer>();
                        c.AddConsumer<DoItJobConsumer>(typeof(DoItJobConsumerDefinition));

                        c.AddSagaRepository<JobSaga>()
                            .EntityFrameworkRepository(r =>
                            {
                                r.ExistingDbContext<JobServiceSagaDbContext>();
                                r.LockStatementProvider = new PostgresLockStatementProvider();
                            });
                        c.AddSagaRepository<JobTypeSaga>()
                            .EntityFrameworkRepository(r =>
                            {
                                r.ExistingDbContext<JobServiceSagaDbContext>();
                                r.LockStatementProvider = new PostgresLockStatementProvider();
                            });
                        c.AddSagaRepository<JobAttemptSaga>()
                            .EntityFrameworkRepository(r =>
                            {
                                r.ExistingDbContext<JobServiceSagaDbContext>();
                                r.LockStatementProvider = new PostgresLockStatementProvider();
                            });

                        c.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.UseRabbitMqMessageScheduler();

                            var options = new ServiceInstanceOptions()
                                    .EnableInstanceEndpoint()
                                    // ServiceInstanceOptions 을 위해 따로 해줘야 하나. @global-name-formatter 에서 했는데..
                                    .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance)
                                // .EnableJobServiceEndpoints() <---- ????
                                ;
                            cfg.ServiceInstance(options,
                                instance =>
                                {
                                    instance.ConfigureJobServiceEndpoints(js =>
                                    {
                                        js.SagaPartitionCount = 1;
                                        js.FinalizeCompleted = true;
                                        js.ConfigureSagaRepositories(context);
                                    });

                                    instance.ConfigureEndpoints(context);
                                });
                            cfg.ConfigureEndpoints(context);
                        });
                    });
                    services.AddHostedService<Worker>();
                });
    }

    public class CustomSagaRepository : ISagaRepository<JobSaga>
    {
        public void Probe(ProbeContext context)
        {
            throw new NotImplementedException();
        }

        public Task Send<T>(ConsumeContext<T> context, ISagaPolicy<JobSaga, T> policy, IPipe<SagaConsumeContext<JobSaga, T>> next) where T : class
        {
            throw new NotImplementedException();
        }

        public Task SendQuery<T>(ConsumeContext<T> context, ISagaQuery<JobSaga> query, ISagaPolicy<JobSaga, T> policy, IPipe<SagaConsumeContext<JobSaga, T>> next) where T : class
        {
            throw new NotImplementedException();
        }
    }
}