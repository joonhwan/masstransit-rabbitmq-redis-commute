using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MassTransit;
using MassTransit.Definition;
using MassTransit.MongoDbIntegration.MessageData;
using Microsoft.Extensions.Logging;
using Sample.Components;
using Sample.Components.Consumers;
using Sample.Contracts;

namespace Sample.Api
{
    public class Startup
    {
        private  ILogger<Startup> _logger;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();                
            });

            _logger = loggerFactory.CreateLogger<Startup>();
            
            services.AddMassTransit(configurator =>
            {
                //configurator.AddConsumer<SubmitOrderConsumer>();
                //configurator.AddMediator();
                configurator.AddBus(provider =>
                {
                    return Bus.Factory.CreateUsingRabbitMq(sbc =>
                    {
                        sbc.Host("rabbitmq://admin:mirero@localhost:5672");
                        sbc.ConfigureEndpoints(provider, KebabCaseEndpointNameFormatter.Instance);
                        sbc.UseHealthCheck(provider);

                        // MessageData<T> 처리를 위한 설정...
                        // --> MessageData<T> 는 PDF, IMAGE.. 등 덩치큰 데이터를 메시지에 실어보내기 위한 방법.
                        // --> 중간 저장소(Repository)를 사용하는 방식. 
                        // -->https://masstransit-project.com/usage/message-data.html 참고.
                        {
                            // the default value is 4096
                            MessageDataDefaults.Threshold = 8192;
                            // 무조건 Repository 에 기록하던 이전 버젼과의 호환성을 위해 아래 값은 true 가 기본값.
                            // 하지만, 이걸 쓰면, MessageDataDefaults.Threshold 를 넘지 않는 MessageData<T> 데이터도 
                            // 항상 Repository(이 경우 MongoDB GridFS)에 기록이 되는 단점이 있다. 
                            // 이걸 False 로 하면, Threshold 를 넘는 MessageData<T> 형 데이터만 Repository에 기록되고, 
                            // Threshold보다 작은 경우에는 메시지 Payload에 내용이 기록된다. 
                            MessageDataDefaults.AlwaysWriteToRepository = false;

                            // 아래 처럼 Repository 에 저장된 데이터의 Time-To-Live 설정을 할 수 있다. 
                            // 하지만,Masstransit에서는 메시지 자체가 TTL 이 적용된 경우(어떻게?..는 ???),
                            // 메시지의 TTL을 따라 적용되는 방식이 자연스럽다(이 경우, 기본값 NULL 을 그냥 써야 한다
                            //
                            // MessageDataDefaults.TimeToLive = TimeSpan.FromMinutes(30);

                            // 원래 Time To Live 설정에 시스템 처리 지연등을 고려한 Extra Time To Live 를 적용할 수 있다. 
                            // MessageDataDefaults.ExtraTimeToLive = TimeSpan.FromHours(1);
                            
                            // MongoDB의 GridFS 를 사용하여 구현된 것으로 보인다.
                            sbc.UseMessageData(new MongoDbMessageDataRepository("mongodb://localhost:27017",
                                "attachments"));
                        }
                    });
                });
                
                // request client 를 di ... 다양한 방법.. 😁
                var uriName = KebabCaseEndpointNameFormatter.Instance.Consumer<SubmitOrderConsumer>();
                _logger.LogInformation($"UriName : {uriName}");
                configurator.AddRequestClient<SubmitOrder>(new Uri($"exchange:{uriName}"),TimeSpan.FromDays(3));
                configurator.AddRequestClient<CheckOrder>();
            });
            services.AddMassTransitHostedService();
            
            services.AddOpenApiDocument(settings =>
            {
                settings.PostProcess = d => d.Info.Title = "Sample API Site";
            });
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}