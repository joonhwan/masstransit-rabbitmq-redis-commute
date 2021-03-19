using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Components;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Services.SubmitClaim
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
                        c.AddConsumer<SubmitClaimConsumer>();
                    });
                });
    }
}