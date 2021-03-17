using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Automatonymous;
using Library.Components.Services;
using Library.Components.StateMachines;
using Library.Components.Tests.Mocks;
using Library.Contracts.Messages;
using Library.TestKit;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Library.Components.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ThankYouSaga는 : StateMachineTestFixture<ThankYouStateMachine, ThankYouSaga>
    {
        protected override void ConfigureServices(ServiceCollection services)
        {
        }

        [Test]
        public async Task BookReserved_와_BookCheckedOut_을_수신하면_Ready_상태가_된다()
        {
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                ReservationId = reservationId,
                Timestamp = InVar.Timestamp,
                Duration = TimeSpan.FromDays(14),
                MemberId = memberId,
                BookId = bookId
            });
            var message = TestHarness.Published.Select<BookReserved>().Last();
            var messageId = message.Context.MessageId ?? Guid.Empty;
            Assert.That(messageId, Is.Not.EqualTo(Guid.Empty));
            Assert.IsTrue(await TestHarness.Consumed.Any<BookReserved>(), "Bus 메시지 수신안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookReserved>(), "Saga에서 메시지 수신안됨");

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId
            });

            await Task.Delay(200); // TODO 어떻게 이런 Sleep 을 하지 않을 수 있을까. 이게 없으면 아래 수신 테스트가 실패함. 
            Assert.IsTrue(await TestHarness.Consumed.Any<BookCheckedOut>(), "Bus 메시지 수신안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(), "Saga에서 메시지 수신안됨");
            
            var saga = SagaHarness.SagaOf(messageId);
            Assert.IsTrue(await saga.Exists(), "Saga 생성되지 않음");
            Assert.IsTrue(await saga.ExistsAs(m => m.Ready), "Saga가 Ready 상태가 아님");

            Assert.That(saga.Instance.BookId, Is.EqualTo(bookId));
            Assert.That(saga.Instance.MemberId, Is.EqualTo(memberId));
            Assert.That(saga.Instance.ReservationId, Is.EqualTo(reservationId));
        }
        
        [Test]
        public async Task BookCheckedOut_과_BookReserved_을_수신하면_Ready_상태가_된다()
        {
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId
            });
            var message = TestHarness.Published.Select<BookCheckedOut>().Last();
            var messageId = message.Context.MessageId ?? Guid.Empty;
            Assert.That(messageId, Is.Not.EqualTo(Guid.Empty));
 
            Assert.IsTrue(await TestHarness.Consumed.Any<BookCheckedOut>(), "Bus 메시지 수신안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(), "Saga에서 메시지 수신안됨");
            await TestHarness.Bus.Publish<BookReserved>(new
            {
                ReservationId = reservationId,
                Timestamp = InVar.Timestamp,
                Duration = TimeSpan.FromDays(14),
                MemberId = memberId,
                BookId = bookId
            });
            await Task.Delay(200); // TODO 어떻게 이런 Sleep 을 하지 않을 수 있을까. 이게 없으면 아래 수신 테스트가 실패함.
            Assert.IsTrue(await TestHarness.Consumed.Any<BookReserved>(), "Bus 메시지 수신안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookReserved>(), "Saga에서 메시지 수신안됨");

            var saga = SagaHarness.SagaOf(messageId);
            Assert.IsTrue(await saga.Exists(), "Saga 생성되지 않음");
            Assert.IsTrue(await saga.ExistsAs(m => m.Ready), "Saga가 Ready 상태가 아님");

            Assert.That(saga.Instance.BookId, Is.EqualTo(bookId));
            Assert.That(saga.Instance.MemberId, Is.EqualTo(memberId));
            Assert.That(saga.Instance.ReservationId, Is.EqualTo(reservationId));
        }

    }

    public static class InMemoryTestHarnessExtensions
    {
        public static Task<bool> WaitForConsuming<TMessage>(this InMemoryTestHarness me) where TMessage : class
        {
            return me.Consumed.Any<TMessage>();
        }
    }

}