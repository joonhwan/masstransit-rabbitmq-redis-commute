using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Library.Components.StateMachines;
using Library.Contracts;
using Library.Contracts.Messages;
using Library.TestKit;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Logging;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Library.Components.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ReservationSaga는_Reservation요청이_오면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
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
    public class ReservationSaga는_대출가능한_책에_대하여_Reservation_요청이_오면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
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
    public class ReservationSaga는_Reserved상태에서_Reservation이_만기되면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
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
            
            var reservationSaga = SagaHarness.SagaOf(reservationId);
            var bookSaga = BookSagaHarness.SagaOf(bookId);
            Assert.IsTrue(await reservationSaga.ExistsAs(m => m.Reserved), "ReservationSaga 가 Reserved 상태가 아님");
            // Assert.IsTrue((await SagaHarness.Exists(reservationId, machine => machine.Reserved)).HasValue,
            //     "ReservationSaga 가 Reserved 상태가 아님");

            Assert.IsTrue(await bookSaga.ExistsAs(m => m.Reserved), "BookSaga 가  Reserved 상태가 아님.");
            // Assert.IsTrue((await BookSagaHarness.Exists(bookId, machine => machine.Reserved)).HasValue, 
            //     "BookSaga 가  Reserved 상태가 아님.");

            await Time.Advance(TimeSpan.FromDays(1));

            //await Task.Delay(5_000);
            
            // TODO CHECK 아래에서 SagaHarness.Exists() 구문을 써서 reservation 인 것이 없는지 확인하는 방식으로는 테스트 불가!!!
            // TODO --> 잘 생각해보면, 아주 짧은 시간동안에는 아직 Schedule 된 메시지가 처리가 미처 안된상태이므로..
            // TODO --> 따라서, 이런 경우에는 SagaHarness.NotExists() 구문을 사용해야 harness 가 "대기" 를 할 수 있음. 
            // existId = await SagaHarness.NotExists(reservationId);
            // Assert.IsFalse(existId.HasValue, 
            //     "ReservationSaga 가 아직도 Reserved 상태임");
            // Assert.IsTrue((await BookSagaHarness.Exists(bookId, machine => machine.Available)).HasValue,
            //     "BookSaga 가 Available 상태가 아님.");
            Assert.IsTrue(await reservationSaga.NotExists());
            Assert.IsTrue(await bookSaga.ExistsAs(m => m.Available));
        }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ReservationSaga는_2일짜리_BookReserved된_Reserved상태에서_만기되면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
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
                BookId = bookId,
                Duration = TimeSpan.FromDays(2)
            });

            // 실제로 Message를 Serialize 하고, 전송하는 모든 과정이 Simulation 된다.
            
            // 이런식으로 하면, Timing Issue가 있을 수 있다.
            // Assert.IsNotNull(SagaHarness.Sagas.ContainsInState(reservationId, StateMachine, x => x.Reserved));
            
            var reservationSaga = SagaHarness.SagaOf(reservationId);
            var bookSaga = BookSagaHarness.SagaOf(bookId);
            
            Assert.IsTrue(await reservationSaga.ExistsAs(m => m.Reserved), "ReservationSaga 가 Reserved 상태가 아님");
            Assert.IsTrue(await bookSaga.ExistsAs(m => m.Reserved), "BookSaga 가  Reserved 상태가 아님.");
            
            await Time.Advance(TimeSpan.FromDays(1));

            Assert.IsTrue(await reservationSaga.ExistsAs(m => m.Reserved),
                "ReservationSaga 는 2일 중 1일만 지났으므로, 아직 Reserved 상태여야 함");
            Assert.IsTrue(await bookSaga.ExistsAs(m => m.Reserved),
                "BookSaga 는 여전히 Reserved 상태여야 함(2일이 만료일인데, 1일만 지난상태에서..)");

            await Time.Advance(TimeSpan.FromDays(1)); // 하루 더 지나면...

            Assert.IsTrue(await reservationSaga.NotExists(),
                "ReservationSaga 는 Reserve 만기되면 사라져야 함.");
            Assert.IsTrue(await bookSaga.ExistsAs(m => m.Available),
                "BookSaga 는 2일뒤 Available상태로 돌아와야 함.");

        }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ReservationSaga는_Reserved상태에서_CheckOut_되면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
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
        public async Task Reserved상태에서_Finalize상태로_되어야함()
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
            
            var reservationSaga = SagaHarness.SagaOf(reservationId);
            var bookSaga = BookSagaHarness.SagaOf(bookId);
            Assert.IsTrue(await reservationSaga.ExistsAs(m => m.Reserved), "ReservationSaga 가 Reserved 상태가 아님");
            Assert.IsTrue(await bookSaga.ExistsAs(m => m.Reserved), "BookSaga 가  Reserved 상태가 아님.");

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                BookId = bookId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId
            });
            
            Assert.IsTrue(await reservationSaga.NotExists());
            Assert.IsTrue(await bookSaga.ExistsAs(m => m.CheckedOut));
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ReservationSaga는_Reserved된_책에_대하여_중복_Reservation요청이_오면 : StateMachineTestFixture<ReservationStateMachine, ReservationSaga>
    {
        [Test]
        public async Task 중복해서_Book_Reserve_하지_않는다()
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

            var reservationSaga = SagaHarness.SagaOf(reservationId);
            var bookSaga = BookSagaHarness.SagaOf(bookId);
            Assert.IsTrue(await reservationSaga.ExistsAs(m => m.Reserved), "ReservationSaga 가 Reserved 상태가 아님");
            Assert.IsTrue(await bookSaga.ExistsAs(m => m.Reserved), "BookSaga 가 Reserved 상태가 아님.");
            
            // 중복 ReservationRequested!
            var reservationId2 = NewId.NextGuid();
            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId2,
                Timestamp = bookRequestedAt,
                MemberId = memberId,
                BookId = bookId
            });

            var reservationSaga2 = SagaHarness.SagaOf(reservationId2);
            Assert.IsTrue(await reservationSaga2.ExistsAs(m => m.Requested), // Not Reserved!
                "이미 Reserve된 책에 대한 중복 Reservation요청에는 Reserved 상태가 되면 안된다");
            
        }

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

    }

}