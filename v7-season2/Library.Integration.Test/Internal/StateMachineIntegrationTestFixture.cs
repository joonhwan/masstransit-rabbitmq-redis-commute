using System.Reflection;
using System.Threading.Tasks;
using Automatonymous;
using Library.Integration.Test.Db;
using Library.TestKit;
using Library.TestKit.Internals;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Library.Integration.Test.Internal
{
    public class StateMachineIntegrationTestFixture<TStateMachine, TInstance> : StateMachineTestFixture<TStateMachine, TInstance>
        where TInstance : class, SagaStateMachineInstance 
        where TStateMachine : class, SagaStateMachine<TInstance>
    {
        private readonly IntegrationTestSagaDbContextFactory _factory;
        
        public StateMachineIntegrationTestFixture()
        {
            _factory = new IntegrationTestSagaDbContextFactory();
        }

        protected override async Task BeforeSetup(ServiceCollection services)
        {
            await MigrationUp();

            services.AddDbContext<TestDbContext>(x =>
            {
                _factory.Apply(x);
            });
        }

        protected override async Task AfterTearDown()
        {
            await MigrationDown();
        }

        protected override void ConfigureSaga(ISagaRegistrationConfigurator<TInstance> cfg)
        {
            cfg.EntityFrameworkRepository(x =>
                {
                    // 음냐 Sqlite 쓸건데, 얘는 어떻게 Lock을 해야하는지 모르게따.
                    //x.LockStatementProvider = new PostgresLockStatementProvider();
                    
                    // Optimistic을 쓰면 LockStatementProvider 자체를 사용 안하니까 괜찮을 듯. 기본값은 Pessimistic
                    x.ConcurrencyMode = ConcurrencyMode.Optimistic;   
                    
                    x.ExistingDbContext<TestDbContext>();
                })
                ;
        }

        private async Task MigrationUp()
        {
            await using var context = _factory.CreateDbContext();
            await context.Database.MigrateAsync();
        }

        private async Task MigrationDown()
        {
            await using var context = new IntegrationTestSagaDbContextFactory().CreateDbContext();

            await context.Database.EnsureDeletedAsync();
        }
    }

    internal class IntegrationTestSagaDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext()
        {
            return CreateDbContext(new string[] { });
        }
        public TestDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder();

            Apply(builder);

            var options = builder.Options;
            return new TestDbContext(options);
        }

        public void Apply(DbContextOptionsBuilder builder)
        {
            builder.UseSqlite("Data Source=library.integration.test.db",
                optionsBuilder =>
                {
                    optionsBuilder.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                    optionsBuilder.MigrationsHistoryTable($"__{nameof(TestDbContext)}");
                });
        }
    }
}