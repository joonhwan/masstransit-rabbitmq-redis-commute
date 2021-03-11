using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LongRun.Contracts;
using MassTransit;
using MassTransit.Contracts.JobService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LongRun.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddMassTransit(c =>
            {
                c.SetKebabCaseEndpointNameFormatter();
                c.AddRequestClient<DoIt>(); // DoIt 메시지를 받아 Job을 Submit하기 위해 필요한 Request Client
                c.AddServiceClient(); // Conductor 의 Service Discovery ?? --> JobConsumer 를 위해 필요한듯.
                
                c.UsingRabbitMq((context, configurator) =>
                {
                    // no -op
                });
            });
            services.AddMassTransitHostedService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}