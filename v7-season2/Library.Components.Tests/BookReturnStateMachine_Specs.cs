using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Library.Components.Consumers;
using Library.Components.Services;
using Library.Components.StateMachines;
using Library.Contracts.Messages;
using Library.TestKit;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Quartz;

namespace Library.Components.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class BookReturnSaga가_BookReturn_을_받으면 : StateMachineTestFixture<BookReturnStateMachine, BookReturnSaga>
    {
        private readonly MockFineCharger _fineCharger;

        public BookReturnSaga가_BookReturn_을_받으면()
        {
            _fineCharger = new MockFineCharger();
        }
        
        protected override void ConfigureServices(ServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<IFineCharger>(_fineCharger);
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator cfg)
        {
            base.ConfigureMassTransit(cfg);

            cfg
                .AddConsumer<ChargeMemberFineConsumer>()
                .Endpoint(x => x.Name = "charge-member-fine")
                ;
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task 납기를_넘은_경우_벌금을_물린다(bool fineOverriden)
        {
            var checkOutId = NewId.NextGuid();
            var bookId  = NewId.NextGuid();
            var memberId  = NewId.NextGuid();
            var now = Time.UtcNow;
            var dueDate = now - TimeSpan.FromDays(3);
            var returnedAt = now;
            var messageId = NewId.NextGuid();

            _fineCharger.NextTimeOverride = fineOverriden;
            await TestHarness.Bus.Publish<BookReturned>(new
            {
                CheckOutId = checkOutId,
                Timestamp = InVar.Timestamp,
                BookId = bookId,
                MemberId = memberId,
                DueDate = dueDate,
                ReturnedAt = returnedAt,
                __MessageId = messageId
            });

            // 이렇게 해도 되네.. 콕 찝어서 딱 그 메시지! 라고 하려면, message id 를 사용해야 겠다.
            Assert.IsTrue(await TestHarness.Consumed.Any<BookReturned>(x => x.Context.MessageId == messageId));
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookReturned>(x => x.Context.MessageId == messageId));
            Assert.IsTrue(await TestHarness.Published.Any<ChargeMemberFine>());
            Assert.IsTrue(await TestHarness.Consumed.Any<ChargeMemberFine>());
            if (fineOverriden)
            {
                Assert.IsTrue(await TestHarness.Sent.Any<FineOverriden>());
                Assert.IsTrue(await TestHarness.Consumed.Any<FineOverriden>());
            }
            else
            {
                Assert.IsTrue(await TestHarness.Sent.Any<FineCharged>());
                Assert.IsTrue(await TestHarness.Consumed.Any<FineCharged>());
            }

            var saga = SagaHarness.SagaOf(checkOutId);
            Assert.IsTrue(await saga.ExistsAs(m => m.Complete));
        }
        
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task 납기를_넘은_경우_벌금을_물렸는데_납부가_안되면_오류_처리가_된다(bool fineOverriden)
        {
            var checkOutId = NewId.NextGuid();
            var bookId  = NewId.NextGuid();
            var memberId  = NewId.NextGuid();
            var now = Time.UtcNow;
            var dueDate = now - TimeSpan.FromDays(3);
            var returnedAt = now;
            var messageId = NewId.NextGuid();

            _fineCharger.NextTimeShouldFail = true;
            _fineCharger.NextTimeOverride = fineOverriden;
            
            await TestHarness.Bus.Publish<BookReturned>(new
            {
                CheckOutId = checkOutId,
                Timestamp = InVar.Timestamp,
                BookId = bookId,
                MemberId = memberId,
                DueDate = dueDate,
                ReturnedAt = returnedAt,
                __MessageId = messageId
            });

            // 이렇게 해도 되네.. 콕 찝어서 딱 그 메시지! 라고 하려면, message id 를 사용해야 겠다.
            Assert.IsTrue(await TestHarness.Consumed.Any<BookReturned>(x => x.Context.MessageId == messageId));
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookReturned>(x => x.Context.MessageId == messageId));
            Assert.IsTrue(await TestHarness.Published.Any<ChargeMemberFine>());
            Assert.IsTrue(await TestHarness.Consumed.Any<ChargeMemberFine>());
            Assert.IsTrue(await TestHarness.Sent.Any<Fault<ChargeMemberFine>>());
            Assert.IsTrue(await TestHarness.Consumed.Any<Fault<ChargeMemberFine>>());
            
            var saga = SagaHarness.SagaOf(checkOutId);
            Assert.IsTrue(await saga.ExistsAs(m => m.ChargingFailed));
        }

        public class MockFineCharger : IFineCharger
        {
            public bool NextTimeOverride { get; set; }
            public bool NextTimeShouldFail { get; set; }
            
            public async Task<ChargeResult> Charge(Guid memberId, decimal fineAmount)
            {
                if (NextTimeShouldFail)
                {
                    NextTimeShouldFail = false;
                    throw new InvalidOperationException("어이구. 돈을 못 받았네요.");
                }
                
                // mimic system processing...
                await Task.Delay(1000);

                var result = NextTimeOverride ? ChargeResult.Overriden : ChargeResult.Charged;
                NextTimeOverride = false;
                return result;
            }
        }
    }
}