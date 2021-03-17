using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Library.Components.StateMachines;
using Library.Contracts.Messages;
using Library.Integration.Test.Internal;
using MassTransit;
using MassTransit.Saga;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Library.Integration.Test
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class When_a_book_is_checked_out_via_reservation :
        StateMachineIntegrationTestFixture<ThankYouStateMachine, ThankYouSaga>
    {
        [Test]
        public async Task Should_handle_in_order()
        {
            var sagaId = NewId.NextGuid();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                BookId = bookId,
                MemberId = memberId,
                ReservationId = reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp,
                __MessageId = sagaId // 와우. MessageId 를 Dunder(__) 를 붙여서 설정할 수 있네.
            });

            var repository = Provider.GetRequiredService<ISagaRepository<ThankYouSaga>>();

            Guid? existsId =
                await repository.ShouldContainSagaInState(sagaId, StateMachine, x => x.Active, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                BookId = bookId,
                MemberId = memberId,
                InVar.Timestamp,
                __MessageId = sagaId 
            });

            existsId = await repository.ShouldContainSagaInState(sagaId,
                StateMachine,
                x => x.Ready,
                TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }

        [Test]
        public async Task Should_handle_in_other_order()
        {
            var sagaId = NewId.NextGuid();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                BookId = bookId,
                MemberId = memberId,
                InVar.Timestamp,
                __MessageId = sagaId
            });

            var repository = Provider.GetRequiredService<ISagaRepository<ThankYouSaga>>();

            Guid? existsId =
                await repository.ShouldContainSagaInState(sagaId, StateMachine, x => x.Active, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                BookId = bookId,
                MemberId = memberId,
                ReservationId = reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp,
                __MessageId = sagaId
            });

            existsId = await repository.ShouldContainSagaInState(sagaId,
                StateMachine,
                x => x.Ready,
                TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }
        
        [Test]
        public async Task Should_handle_status_checks()
        {   
            var sagaId = NewId.NextGuid();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            var client = TestHarness.Bus.CreateRequestClient<ThankYouStatusRequested>();
            var response = await client.GetResponse<ThankYouStatus>(new
            {
                MemberId = memberId
            });
            Assert.That(response.Message.Status, Is.EqualTo("NotFound"));

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                BookId = bookId,
                MemberId = memberId,
                InVar.Timestamp,
                __MessageId = sagaId
            });

            var repository = Provider.GetRequiredService<ISagaRepository<ThankYouSaga>>();

            Guid? existsId =
                await repository.ShouldContainSagaInState(sagaId, StateMachine, x => x.Active, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            response = await client.GetResponse<ThankYouStatus>(new
            {
                MemberId = memberId
            });

            Assert.That(response.Message.Status, Is.EqualTo("Active (State)"));

            existsId = await repository.ShouldContainSagaInState(sagaId,
                StateMachine,
                x => x.Active,
                TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                BookId = bookId,
                MemberId = memberId,
                ReservationId = reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp,
                __MessageId = sagaId
            });

            existsId = await repository.ShouldContainSagaInState(sagaId,
                StateMachine,
                x => x.Ready,
                TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");

            response = await client.GetResponse<ThankYouStatus>(new
            {
                MemberId = memberId
            });

            Assert.That(response.Message.Status, Is.EqualTo("Ready (State)"));
        }
    }
}