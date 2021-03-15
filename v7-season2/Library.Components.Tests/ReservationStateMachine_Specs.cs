using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Library.Contracts;
using Library.Contracts.Messages;
using Library.TestKit;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Library.Components.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Reservation요청이_오면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
    {
        [Test]
        public async Task ReservationSaga가_생성된다()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            
            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId
            });

            // 실제로 Message를 Serialize 하고, 전송하는 모든 과정이 Simulation 된다. 
            
            Assert.IsTrue(await TestHarness.Consumed.Any<ReservationRequested>(), "메시지 수신이 안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<ReservationRequested>(), "Saga에 의해 메시지 처리가 안됨");
            Assert.That(await SagaHarness.Created.Any(x => x.CorrelationId == reservationId),
                "생성된 Saga의 CorrelationId 는 Reservation Id 여야 함");

            // var instance = SagaHarness.Created.ContainsInState(bookId, StateMachine, StateMachine.Available);
            // var wrongBookId = NewId.NextGuid();
            Assert.That(await SagaHarness.Exists(reservationId, machine => machine.Requested), Is.EqualTo(reservationId), 
                "수신된 메시지의 BookId 에 해당하는 Requested 상태의 Saga가 없음.");
        }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class 대출가능한_책에_대하여_Reservation_요청이_오면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
    {
        // 여기서는 2개의 StateMachine 이 필요.
        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator cfg)
        {
            base.ConfigureMassTransit(cfg);
            
            // ReservationStateMachine 에, 추가로 BookStateMachine 을 명시적으로 생성.
            cfg.AddSagaStateMachine<BookStateMachine, BookSaga>()
                .InMemoryRepository();
            cfg.AddSagaStateMachineTestHarness<BookStateMachine, BookSaga>();
        }

        [OneTimeSetUp]
        public void SetupBookStateMachine()
        {
            BookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<BookSaga, BookStateMachine>>();
            BookStateMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        public BookStateMachine BookStateMachine { get; set; }
        public IStateMachineSagaTestHarness<BookSaga, BookStateMachine> BookSagaHarness { get; set; }

        [Test]
        public async Task Book을_예약해야_한다()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            var isbn = "1234567";
            var bookAddedAt = new DateTime(2020, 12, 11);
            var bookRequestedAt = new DateTime(2020, 12, 25);

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Timestamp = bookAddedAt,
                Isbn = isbn,
                Title = "Gone with the Wind"
            });

            var existId = await BookSagaHarness.Exists(bookId, machine => machine.Available);
            Assert.IsTrue(existId.HasValue);
            
            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                Timestamp = bookRequestedAt,
                MemberId = memberId,
                BookId = bookId
            });

            // 실제로 Message를 Serialize 하고, 전송하는 모든 과정이 Simulation 된다.
            
            // 이런식으로 하면, Timing Issue가 있을 수 있다.
            // Assert.IsNotNull(SagaHarness.Sagas.ContainsInState(reservationId, StateMachine, x => x.Reserved));
            Assert.That(await SagaHarness.Exists(reservationId, machine => machine.Reserved), Is.EqualTo(reservationId), 
                "ReservationSaga 가 Reserved 상태가 아님");

            
            // Assert.IsNotEmpty(SagaHarness.Sagas.Select(context =>
            //     context.CorrelationId == reservationId &&
            //     context.RequestedAt == bookRequestedAt
            // ));
            
        }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Reserved상태에서_Reservation이_만기되면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
    {
        // 여기서는 2개의 StateMachine 이 필요.
        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator cfg)
        {
            base.ConfigureMassTransit(cfg);
            
            // ReservationStateMachine 에, 추가로 BookStateMachine 을 명시적으로 생성.
            cfg.AddSagaStateMachine<BookStateMachine, BookSaga>()
                .InMemoryRepository();
            cfg.AddSagaStateMachineTestHarness<BookStateMachine, BookSaga>();
        }

        [OneTimeSetUp]
        public void SetupBookStateMachine()
        {
            //BookSagaHarness = Provider.GetRequiredSaga<BookStateMachine, BookSaga>();
            BookSagaHarness =Provider.GetRequiredService<IStateMachineSagaTestHarness<BookSaga, BookStateMachine>>();
            BookStateMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        public BookStateMachine BookStateMachine { get; set; }
        public IStateMachineSagaTestHarness<BookSaga, BookStateMachine> BookSagaHarness { get; set; }

        [Test]
        public async Task Reservation이_더_이상_Reserved_상태가_아님()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            var isbn = "1234567";
            var bookAddedAt = new DateTime(2020, 12, 11);
            var bookRequestedAt = new DateTime(2020, 12, 25);

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Timestamp = bookAddedAt,
                Isbn = isbn,
                Title = "Gone with the Wind"
            });

            var existId = await BookSagaHarness.Exists(bookId, machine => machine.Available);
            Assert.IsTrue(existId.HasValue);
            
            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                Timestamp = bookRequestedAt,
                MemberId = memberId,
                BookId = bookId
            });

            // 실제로 Message를 Serialize 하고, 전송하는 모든 과정이 Simulation 된다.
            
            // 이런식으로 하면, Timing Issue가 있을 수 있다.
            // Assert.IsNotNull(SagaHarness.Sagas.ContainsInState(reservationId, StateMachine, x => x.Reserved));
            Assert.That(await SagaHarness.Exists(reservationId, machine => machine.Reserved), Is.EqualTo(reservationId), 
                "ReservationSaga 가 Reserved 상태가 아님");

            
            // Assert.IsNotEmpty(SagaHarness.Sagas.Select(context =>
            //     context.CorrelationId == reservationId &&
            //     context.RequestedAt == bookRequestedAt
            // ));
            
        }
    }
}