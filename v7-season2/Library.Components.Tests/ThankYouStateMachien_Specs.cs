﻿using System;
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
        public async Task BookReserved_와_BookCheckedOut_을_수신하면_Active상태가_된다()
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
            
            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId
            });
            // 희한하게, Consumed.Any<T>() 는 이런식으로 Arrange-Act-Assert 의 3단계를 막 Cascading하면 안될때가 있다. 
            // 먼가 문제가 있는데... 아무튼, 예제 코드는 모든 메시지 수신여부를 이렇게 확인하고 있다.
            Assert.IsTrue(await TestHarness.Consumed.Any<BookReserved>(), "Bus 메시지 수신안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookReserved>(), "Saga에서 메시지 수신안됨");
            Assert.IsTrue(await TestHarness.Consumed.Any<BookCheckedOut>(), "Bus 메시지 수신안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(), "Saga에서 메시지 수신안됨");
            
            var saga = SagaHarness.SagaOf(messageId);
            Assert.IsTrue(await saga.Exists(), "Saga 생성되지 않음");
            Assert.IsTrue(await saga.ExistsAs(m => m.Active), "Saga가 Active 상태가 아님");

            Assert.That(saga.Instance.BookId, Is.EqualTo(bookId));
            Assert.That(saga.Instance.MemberId, Is.EqualTo(memberId));
            Assert.That(saga.Instance.ReservationId, Is.EqualTo(reservationId));
        }
    }

}