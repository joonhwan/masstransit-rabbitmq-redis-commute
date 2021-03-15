using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Automatonymous;
using Automatonymous.Binders;
using Library.Contracts;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Library.Components
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
            
            Initially(
                When(ReservationRequested)
                    .Then(UpdateData)
                    .TransitionTo(Requested)
            );

            During(Requested,
                When(BookReserved)
                    .Then(UpdateReservedTimestamp)
                    // BookReserved 를 Requested 상태에서 수신하면, ReservationExpired 메시지의 스케쥴을 건다.
                    // .Schedule(ReservationExpiredSchedule,
                    //     context => context.Init<ReservationExpired>(new {context.Data.ReservationId}))
                    .Schedule(ReservationExpiredSchedule,
                        context => context.Init<ReservationExpired>(new {context.Data.ReservationId}),
                        context => context.Data.Duration ?? TimeSpan.FromDays(1))
                    .TransitionTo(Reserved)
            );

            During(Reserved,
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
        
        private void UpdateData(BehaviorContext<ReservationSaga, ReservationRequested> context)
        {
            context.Instance.MemberId = context.Data.MemberId;
            context.Instance.BookId = context.Data.BookId;
            context.Instance.RequestedAt = context.Data.Timestamp;
        }
        
        private void UpdateReservedTimestamp(BehaviorContext<ReservationSaga, BookReserved> context)
        {
            context.Instance.ReservedAt = context.Data.Timestamp;
            // context.Instance.Duration = context.Data.Duration;
        }
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