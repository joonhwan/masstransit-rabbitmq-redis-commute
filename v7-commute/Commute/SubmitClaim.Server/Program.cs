using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommuteSystem;
using CommuteSystem.Consumers;
using MassTransit;
using MassTransit.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SubmitClaim.Server
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
                    services.AddHostedService<Worker>();
                    services.AddMassTransit(c =>
                    {
                        c.AddConsumer<ClaimSubmissionConsumer>();
                        
                        c.UsingRabbitMq((context, configurator) =>
                        {
                            //configurator.Message<CommuteSystem.Contracts.SubmitClaim>(m => m.SetEntityName("Mirero.SubmitClaim"));
                            configurator.MessageTopology.SetEntityNameFormatter(new CustomEntityNameFormatter());
                            
                            configurator.ConfigureEndpoints(context);
                        });
                    });
                });
    }
}