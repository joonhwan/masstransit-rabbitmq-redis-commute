using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Library.Contracts;
using Library.Contracts.Messages;
using Library.TestKit;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Library.Components.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CheckOutSaga는_Book이_CheckOut되면 : StateMachineTestFixture<CheckOutStateMachine, CheckOutSaga>
    {
        protected override void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<CheckOutSettings>(new TestCheckOutSettings());
        }
        
        [Test]
        public async Task 새로운_BookId_메시지를_받으면_새로운_Saga_Instance가_만들어진다()
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
        }
    }

    public class TestCheckOutSettings : CheckOutSettings
    {
        public TimeSpan DefaultCheckOutDuration { get; } = TimeSpan.FromDays(14);
    }
}