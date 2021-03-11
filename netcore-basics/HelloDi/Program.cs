using System;
using HelloDi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HelloDi
{
    class Program
    {
        static void Main(string[] args)
        {
            // 통상 framework이 하나 만들어 주는 service collection. 
            // --> 등록된 type 들의 목록. 
            var services = new ServiceCollection();
            
            services.AddTransient<IGreeter, KoreanGreeter>();
            services.AddTransient<IGreeter, EnglishGreeter>();
            services.TryAddTransient<IGreeter, KoreanGreeter>(); // TryAdd는 중복 등록을 막아준다.  
            
            services.AddScoped<IEventProcessor, EventProcessor>();
            services.AddScoped<IDisposableProcessor, DisposableProcessor>();

            TestRootContainer(services);

            TestScopedContainer(services);


        }

        private static void TestScopedContainer(ServiceCollection services)
        {
            // root container
            var serviceProvider = services.BuildServiceProvider();
            //var scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
            
            // `GetRequiredService` 는 등록안되서 객체가 생성안되어 null이 반환되는 경우, 예외를 낸다.
            //  ..는 점 빼고는 `GetService` 와 동일.
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            Console.WriteLine("Scope 를 만듭니다.");
            
            using (var scope = scopeFactory.CreateScope())
            { 
                var processor = scope.ServiceProvider.GetService<IDisposableProcessor>();
                processor.Process("명시적으로 Dispose를 안해도 Dispose 될 겁니다.");

                var greeter = scope.ServiceProvider.GetService<IGreeter>();
                greeter.Greet();
            }

            Console.WriteLine("Scope가 파괴된 다음입니다.");
            
            Console.WriteLine("Scope 를 다시 만듭니다.");
            
            using (var scope = scopeFactory.CreateScope())
            {
                using (var processor = scope.ServiceProvider.GetService<IDisposableProcessor>())
                {
                    processor.Process("명시적으로 dispose 한 다음에도..Dispose가 될겁니다. 2번...");
                }
            }

            Console.WriteLine("Scope가 다시 파괴된 다음입니다.");
            
        }

        private static void TestRootContainer(ServiceCollection services)
        {
            // root container
            var serviceProvider = services.BuildServiceProvider();
            
            var aGreeter = serviceProvider.GetService<IGreeter>();
            aGreeter.Greet();

            var greeters = serviceProvider.GetServices<IGreeter>();
            foreach (var greeter in greeters)
            {
                greeter.Greet();
            }

            try
            {
                var eventProcessor = serviceProvider.GetService<IEventProcessor>();
                if (eventProcessor == null)
                {
                    Console.WriteLine("어오. EventProcessor를 못만드네요. Scoped 인가요.");
                }
                else
                {
                    eventProcessor.Process("샘플 이벤트");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

}