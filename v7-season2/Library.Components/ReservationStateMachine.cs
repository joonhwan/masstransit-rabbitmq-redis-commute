using System;
using System.Diagnostics.CodeAnalysis;
using Automatonymous;
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
            Event(() => ReservationRequested, x => x.CorrelateById(m => m.Message.ReservationId));
            Event(() => BookReserved, x => x.CorrelateById(m => m.Message.ReservationId));
            Event(() => ReservationExpired, x => x.CorrelateById(m => m.Message.ReservationId));
            
            InstanceState(x => x.CurrentState);

            Schedule(() => ReservationExpiredSchedule, x => x.ReservationExpiredScheduleToken, schedule => schedule.Delay = TimeSpan.FromHours(24));
            
            Initially(
                When(ReservationRequested)
                    .Then(UpdateData)
                    .TransitionTo(Requested)
            );
            
            During(Requested,
                When(BookReserved)
                    .Then(UpdateReservedTimestamp)
                    .Then(context =>
                    {
                        logger.LogInformation("책이 Reserved 됨 : ReservationId={ReservationId}", context.Data.ReservationId);
                    })
                    // BookReserved 를 Requested 상태에서 수신하면, ReservationExpired 메시지의 스케쥴을 건다.
                    //.Schedule(ReservationExpiredSchedule, context => context.Init<ReservationExpired>(new { ReservationId = context.Data.ReservationId,}))
                    .Schedule(ReservationExpiredSchedule, context => context.Init<ReservationExpired>(new {context.Data.ReservationId}))
                    .TransitionTo(Reserved));

            During(Reserved,
                When(ReservationExpired)
                    .Then(context =>
                    {
                        logger.LogWarning("@@@@@@@@@@@@@@ 응? Expire되었네요. @@@@@@@@@@@@@@");
                    })
                    .Finalize()
            );
            
            // Finalize 가 되면, Saga Repository 에서 Finalize 된 현 Saga 를 제거.
            SetCompletedWhenFinalized(); 
        }
        
        public State Requested { get; }
        public State Reserved { get;  }
        
        public Schedule<ReservationSaga, ReservationExpired> ReservationExpiredSchedule { get; }
        
        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<BookReserved> BookReserved { get; }
        public Event<ReservationExpired> ReservationExpired { get; }
        
        private void UpdateData(BehaviorContext<ReservationSaga, ReservationRequested> context)
        {
            context.Instance.MemberId = context.Data.MemberId;
            context.Instance.BookId = context.Data.BookId;
            context.Instance.RequestedAt = context.Data.Timestamp;
        }
        
        private void UpdateReservedTimestamp(BehaviorContext<ReservationSaga, BookReserved> context)
        {
            context.Instance.ReservedAt = context.Data.Timestamp;
        }

    }
}