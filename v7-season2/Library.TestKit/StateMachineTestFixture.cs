using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Automatonymous;
using Library.TestKit.Internals;
using MassTransit;
using MassTransit.Context;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Quartz;

namespace Library.TestKit
{
    public class StateMachineTestFixture<TStateMachine, TInstance>
        where TStateMachine : class, SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        protected ServiceProvider Provider;
        protected InMemoryTestHarness TestHarness;
        protected FakeSystemTime Time;
        protected IStateMachineSagaTestHarness<TInstance, TStateMachine> SagaHarness;
        protected TStateMachine StateMachine;
        
        [OneTimeSetUp]
        public async Task Setup()
        {
            var services = new ServiceCollection();

            // NUnit 출력으로 Log 를 내보내는 Logger 생성기.(모든 Log Level 이 전부 다 Enable 된
            //  Masstransit.TestFramework.Logging.TestOutputLoggerFactory (NUnit의 실행환경하에서 출력을 뿜어내는 Logger를 만든다)
            services.AddSingleton<ILoggerFactory>(provider => new TestOutputLoggerFactory(enabled: true));
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
            
            // @register-test-harness
            services.AddMassTransitInMemoryTestHarness(cfg =>
            {
                cfg.SetKebabCaseEndpointNameFormatter();
                
                cfg.AddPublishMessageScheduler();

                // @register-state-machine
                cfg.AddSagaStateMachine<TStateMachine, TInstance>()
                    .InMemoryRepository()
                    ;
                
                //@register-saga-test-harness
                cfg.AddSagaStateMachineTestHarness<TStateMachine, TInstance>(); 

                ConfigureMassTransit(cfg);
            });
            
            ConfigureServices(services);

            Provider = services.BuildServiceProvider();

            var loggerFactory = Provider.GetRequiredService<ILoggerFactory>(); 
            ConfigureLogging(loggerFactory);

            // see @register-test-harness
            TestHarness = Provider.GetRequiredService<InMemoryTestHarness>();
            Time = FakeSystemTime.For(TestHarness);
            
            await TestHarness.Start();

            // see @register-saga-test-harness  
            SagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<TInstance, TStateMachine>>();
            // see @register-state-machine
            StateMachine = Provider.GetRequiredService<TStateMachine>();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            try
            {
                await TestHarness.Stop();
            }
            finally
            {
                Time.Dispose();
                await Provider.DisposeAsync();
            }
        }
        
        protected virtual void ConfigureLogging(ILoggerFactory loggerFactory)
        {
            // masstransit의 logging이 test용 logger factory 를 사용하도록 지정.
            LogContext.ConfigureCurrentLogContext(loggerFactory);
            
            // Quartz 의 logging이  test용 logger factory 를 사용하도록 지정.
            Quartz.Logging.LogContext.SetCurrentLogProvider(loggerFactory);
        }

        protected virtual void ConfigureMassTransit(IServiceCollectionBusConfigurator cfg)
        {
        }

        protected virtual void ConfigureServices(ServiceCollection services)
        {
            // no-op
        }
    }
}