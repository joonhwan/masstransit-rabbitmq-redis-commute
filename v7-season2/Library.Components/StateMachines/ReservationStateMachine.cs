using System;
using System.Diagnostics.CodeAnalysis;
using Automatonymous;
using Automatonymous.Binders;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Library.Components.StateMachines
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    public class ReservationStateMachine : MassTransitStateMachine<ReservationSaga>
    {
        public ReservationStateMachine(ILogger<ReservationStateMachine> logger)
        {
            Event(() => BookReserved, x => x.CorrelateById(m => m.Message.ReservationId));
            Event(() => BookCheckedOut, x => x.CorrelateBy((saga, context) => saga.BookId == context.Message.BookId));
            Event(() => ReservationRequested, x => x.CorrelateById(m => m.Message.ReservationId));
            Event(() => ReservationExpired, x => x.CorrelateById(m => m.Message.ReservationId));
            Event(() => ReservationCancellationRequested, x => x.CorrelateById(m => m.Message.ReservationId));
            
            InstanceState(x => x.CurrentState);

            Schedule(() => ReservationExpiredSchedule, x => x.ReservationExpiredScheduleToken, schedule => schedule.Delay = TimeSpan.FromDays(1));
            
            // 이 State machine 은  Message Out-of-Ordering 문제가 발생할 수 있다는 가정을 하고 작성됨. 
            // 즉, ReservationRequested -> BookReserved -> BookCheckedOut 이 정상순서이지만, 
            //     BookReserved -> ReservationRequested -> BookCheckedOut 순으로 들어올 수도 있다고 가정함.  

            Initially(
                When(ReservationRequested)
                    .Then(context =>
                    {
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.RequestedAt = context.Data.Timestamp;
                    })
                    .TransitionTo(Requested),
                // TODO CHECKME 메시지는 순서가 뒤죽박죽 들어올 수 있다. 심지어.. ReservationExpired 도???. 
                // 그렇다고, 아래처럼 하면 될까? 
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.ReservedAt = context.Data.Timestamp;
                    })
                    .Schedule(ReservationExpiredSchedule,
                        context => context.Init<ReservationExpired>(new {context.Data.ReservationId}),
                        context => context.Data.Duration ?? TimeSpan.FromDays(1))
                    .TransitionTo(Reserved)
                ,
                When(ReservationExpired)
                    .Finalize()
            );

            During(Requested,
                // 중복으로 들어온 ReservationRequested 은 무시. 
                When(ReservationRequested),
                // --- 
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.ReservedAt = context.Data.Timestamp;
                    })
                    // BookReserved 를 Requested 상태에서 수신하면, ReservationExpired 메시지의 스케쥴을 건다.
                    // .Schedule(ReservationExpiredSchedule,
                    //     context => context.Init<ReservationExpired>(new {context.Data.ReservationId}))
                    .Schedule(ReservationExpiredSchedule,
                        context => context.Init<ReservationExpired>(new {context.Data.ReservationId}),
                        context => context.Data.Duration ?? TimeSpan.FromDays(1))
                    .TransitionTo(Reserved)
            );

            During(Reserved,
                //  BookReserved가 중복으로 온 경우, 
                When(BookReserved)
                    // Re-schedule 은 harmless. Scheduler는 동일한 id(??) 에 대한 중복 Scheduling 에 대하여 시간만 새로 Reset한단다...
                    .Schedule(ReservationExpiredSchedule,
                        context => context.Init<ReservationExpired>(new {context.Data.ReservationId}),
                        context => context.Data.Duration ?? TimeSpan.FromDays(1)),
                Ignore(ReservationRequested), 
                // --- 나머지
                When(BookCheckedOut)
                    .Finalize(),
                When(ReservationExpired)
                    .PublishReservationCanceled()
                    .Finalize(),
                When(ReservationCancellationRequested)
                    .PublishReservationCanceled()
                    // 강제로 예약 취소했으므로, 만료 대기중인 Schedule은 더이상 필요없다
                    .Unschedule(ReservationExpiredSchedule)
                    .Finalize()
            );
            
            WhenEnterAny(binder => binder.Then(context =>
            {
                logger.LogInformation("Reservation Saga 상태 변경됨 : {State}", context.Instance.CurrentState);
            }));
            
            // Finalize 가 되면, Saga Repository 에서 Finalize 된 현 Saga 를 제거.
            SetCompletedWhenFinalized(); 
        }
        

        public State Requested { get; }
        public State Reserved { get;  }
        
        public Schedule<ReservationSaga, ReservationExpired> ReservationExpiredSchedule { get; }
        
        public Event<BookReserved> BookReserved { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }
        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<ReservationExpired> ReservationExpired { get; }
        public Event<ReservationCancellationRequested> ReservationCancellationRequested { get; }
    }
    
    public static class ReservationStateMachineExtensions
    {
        public static EventActivityBinder<ReservationSaga, TMessage> PublishReservationCanceled<TMessage>(
            this EventActivityBinder<ReservationSaga, TMessage> me)
            where TMessage : class
        {
            return me.PublishAsync(context => context.Init<BookReservationCanceled>(new
            {
                BookId = context.Instance.BookId,
                ReservationId = context.Instance.CorrelationId
            }));
        }
    }
}