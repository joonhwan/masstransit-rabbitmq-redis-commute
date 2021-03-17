using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Automatonymous;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;
using Quartz.Logging;

namespace Library.Components.StateMachines
{
    // CompositeEvent 의 Demo StateMachine
    //
    // `seal` 된 것은 CompositeEvent() 때문임.
    [SuppressMessage("ReSharper", "MCA0001")]
    public sealed class ThankYouStateMachine : MassTransitStateMachine<ThankYouSaga>
    {
        public ThankYouStateMachine(ILogger<ThankYouStateMachine> logger)
        {
            InstanceState(x => x.CurrentState);

            Event(() => BookReserved,
                x =>
                {
                    x.CorrelateBy((state, context) =>
                            state.BookId == context.Message.BookId && context.Message.MemberId == state.MemberId)
                        .SelectId(context => context.MessageId ?? NewId.NextGuid());

                    x.InsertOnInitial = true;
                });
            Event(() => BookCheckedOut,
                x => x.CorrelateBy((state, context) =>
                        state.BookId == context.Message.BookId && state.MemberId == context.Message.MemberId)
                    .SelectId(context => context.MessageId ?? NewId.NextGuid())
            );
            Event(() => ThankYouStatusRequested,
                x =>
                {
                    x.CorrelateBy((saga, context) => saga.MemberId == context.Message.MemberId);
                    //x.ReadOnly = true
                    
                    // 만일 주어진 memberId 를 가지는 Saga가 존재하지 않으면, NotFound 를 응답.
                    x.OnMissingInstance(m => m.ExecuteAsync(context => context.RespondAsync<ThankYouStatus>(new
                    {
                        Status = "NotFound",
                        MemberId = default(Guid), // 음. instance가 없으므로... 값도 가져올 데가 읍다.
                        BookId = default(Guid)    // 음. 
                    })));
                }
            );

            Initially(
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.ReservationId = context.Data.ReservationId;
                    })
                    .TransitionTo(Active),
                When(BookCheckedOut)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                    })
                    .TransitionTo(Active)
            );

            During(Active,
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.ReservationId = context.Data.ReservationId;
                    }),
                // BookReserved 가 먼저 수신된 상태에서 BookCheckedOut 이 이어서 수신된 경우,
                // 이미 BookId, MemberId, ReservationId 가 다 설정된 상태이므로.. 따로 할게 읍다.
                //
                // 또한, 이벤트를 명시적으로 Ignore() 하면, Masstransit이 해당 메시지를 처리안했다는
                // 경고성 로그가 사라진다.
                Ignore(BookCheckedOut)  
            );

            // DuringAny() 는 Initial 상태는 포함하지 않는다!!!!
            DuringAny(
                When(ThankYouStatusRequested)
                    .RespondAsync(context => context.Init<ThankYouStatus>(new
                    {
                        MemberId = context.Instance.MemberId,
                        BookId = context.Instance.BookId,
                        // @masstransit-initializer-async-type-mapping
                        //
                        // Masstransit Initializer는
                        //   `ThankYouState` 메시지의 `Status`의 Type이 `String`임을 확인하고, 
                        //   -  Task<State<ThankYouSaga>> --> State<ThankYouSaga> 대기 후,
                        //   -  State<ThankYouSaga> --> .toString() 으로 string 변환.
                        // ..의 과정이 자동으로 이루어진다.
                        Status = GetStatus(context), 
                    }))
            );
            
            // 다른 Event() 메소드 바로 밑에 두지 않고 여기에 둔 이유가 있음. 
            // see : https://youtu.be/zLn9WPM2ExY?t=1215 
            CompositeEvent(() => ReadyToThank,
                saga => saga.ThankYouStatus,
                CompositeEventOptions.IncludeInitial, // TODO 이거 빼먹지 않게 주의.  -,.-
                BookReserved,
                BookCheckedOut);

            DuringAny(
                When(ReadyToThank)
                    .Then(_ =>
                    {
                        logger.LogInformation("********* ReadyToThank ***********");
                    })
                    .TransitionTo(Ready)
            );
        }

        private Task<State<ThankYouSaga>> GetStatus(ConsumeEventContext<ThankYouSaga, ThankYouStatusRequested> context)
        {   
            var sm = this as StateMachine<ThankYouSaga>;
            return sm.Accessor.Get(context);
        }

        public State Active { get; }
        public State Ready { get; }
        
        public Event<BookReserved> BookReserved { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }
        public Event<ThankYouStatusRequested> ThankYouStatusRequested { get; }
        public Event ReadyToThank { get; }
    }

    public class ThankYouSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid MemberId { get; set; }
        public Guid BookId { get; set; }
        public string CurrentState { get; set; }
        public Guid? ReservationId { get; set; }
        public int ThankYouStatus { get; set; }
    }
}