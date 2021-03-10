﻿using System.Reflection;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using MassTransit.Definition;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using TrashLantis.Components;
using TrashLantis.Components.Consumers;
using TrashLantis.Components.StateMachines;
using TrashLantis.Contracts;

namespace TrashLantis.Service
{
    public static class Program
    {
        static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<IMessageValidator<EmptyTrashBin>, MessageValidator<EmptyTrashBin>>();
                    services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<TrashConsumer>(typeof(TrashConsumerDefinition));

                        x.AddSagaStateMachine<TrashRemovalStateMachine, TrashRemovalState>(typeof(TrashRemovalSagaDefinition))
                            .EntityFrameworkRepository(r =>
                            {
                                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;

                                r.AddDbContext<DbContext, TrashRemovalStateDbContext>((provider, optionsBuilder) =>
                                {
                                    optionsBuilder.UseSqlServer(hostContext.Configuration.GetConnectionString("service"), m =>
                                    {
                                        m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                                        m.MigrationsHistoryTable($"__{nameof(TrashRemovalStateDbContext)}");
                                    });
                                });
                            });

                        x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                        {
                            cfg.ConfigureEndpoints(provider);
                            // cfg.UseFilter(new ConsoleConsumeFilter());
                        }));
                    });

                    services.AddSingleton<IHostedService, MassTransitConsoleHostedService>();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddSerilog(dispose: true);
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                });

            await builder.RunConsoleAsync();

            Log.CloseAndFlush();
        }
    }
}