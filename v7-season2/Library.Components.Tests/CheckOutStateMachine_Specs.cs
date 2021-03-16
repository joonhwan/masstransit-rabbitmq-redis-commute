using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Library.Components.Services;
using Library.Components.StateMachines;
using Library.Components.Tests.Mocks;
using Library.Contracts;
using Library.Contracts.Messages;
using Library.TestKit;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Logging;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Library.Components.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CheckOutSaga는_Book이_CheckOut되면 : StateMachineTestFixture<CheckOutStateMachine, CheckOutSaga>
    {
        protected override void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<CheckOutSettings>(new TestCheckOutSettings());
            services.AddScoped<IMemberRegistry>(provider => new MockMemberRegistry(true));
        }

        [Test]
        public async Task 새로운_BookId_메시지를_받으면_새로운_Saga_Instance가_만들어지고_Activity가_수행된다()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId
            });

            // 실제로 Message를 Serialize 하고, 전송하는 모든 과정이 Simulation 된다. 
            
            Assert.IsTrue(await TestHarness.Consumed.Any<BookCheckedOut>(), "메시지 수신이 안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(), "메시지 수신이 안됨");
            
            var checkOutSaga = SagaHarness.SagaOf(checkOutId);
            Assert.IsTrue(await checkOutSaga.Created(), "Saga 생성 안됨");
            Assert.IsTrue(await checkOutSaga.ExistsAs(m => m.CheckedOut), "CheckOut 상태가 아님.");

            Assert.IsTrue(await TestHarness.Published.Any<NotifyMemberDueDate>(), "NotifyMemberDueDate 메시지 publish 안됨");
        }
    }

    public class TestCheckOutSettings : CheckOutSettings
    {
        public TimeSpan DefaultCheckOutDuration { get; } = TimeSpan.FromDays(14);
    }
}