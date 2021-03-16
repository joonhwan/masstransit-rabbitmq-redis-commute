using System;
using System.Linq;
using Automatonymous;
using Library.Components.Activities;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Library.Components.StateMachines
{
    public class CheckOutStateMachine : MassTransitStateMachine<CheckOutSaga>
    {
        private readonly ILogger<CheckOutStateMachine> _logger;

        public CheckOutStateMachine(CheckOutSettings settings, ILogger<CheckOutStateMachine> logger)
        {
            _logger = logger;
            Event(() => BookCheckedOut, x => x.CorrelateById(m => m.Message.CheckOutId));
            Event(() => RenewCheckOut, x =>
            {
                x.CorrelateById(m => m.Message.CheckOutId);
                
                // // 기존에 CheckOutId 에 해당하는 Saga가 없는 상태에서 RenewCheckOut 이 수신되면, 
                // // Saga를 생성하는 대신, OnMissingInstance() 로 특별한 처리를 할 수 있다
                // // 아래의 경우 *_error queue 로 빠진다. (그 과정에서 Retry Policy 에 따라 재시도도 이루어지게 할 수 있다)
                // x.OnMissingInstance(m => m.Fault());

                // 사용자 경험을 좋게(~ Error가 발생되었다는 로그, 큐가 생성하지 않고)
                // 명시적으로 어떤 특별한 작업(예: 불완전한 상태에 대한 기술을 하는 메시지를 전송) 을 수행.
                x.OnMissingInstance(m => m.ExecuteAsync(async context =>
                {
                    // Publish, Respond, ....
                    await context.RespondAsync<CheckOutNotFound>(new
                    {
                        CheckOutId = context.Message.CheckOutId
                    });
                }));
            });
            
            InstanceState(saga => saga.CurrentState);

            Initially(
                When(BookCheckedOut)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.CheckOutDate = context.Data.Timestamp;
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.DueDate = context.Instance.CheckOutDate + settings.DefaultCheckOutDuration;
                    })
                    // DI Container 가 지원되도록 NotifyMemberActivity 를 생성/사용하려면. 아래처럼 하면 된다.
                    .Activity(x => x.OfInstanceType<NotifyMemberActivity>())
                    .TransitionTo(CheckedOut)
            );

            During(CheckedOut,
                When(RenewCheckOut)
                    .Then(context =>
                    {
                        // 현재 시각을 기준으로 기한을 갱신한다.
                        // --> 시간 관련된 것은 테스트 하기 어렵다. 따라서 Quartz 같은 애들이 제공하는 SystemTime 처럼 Mock이 용이한 것을 쓰는게 좋을듯.
                        var now = SystemTime.UtcNow().DateTime;
                        context.Instance.DueDate = now + settings.DefaultCheckOutDuration;
                    })
                    // 꼭 `.IfElse()` 를 쓰지 않아도 되지만, 이렇게 하면 상태기계 시각화 모듈에 분기조건이 표시된다.
                    .IfElse(
                        context =>
                            context.Instance.DueDate >
                            context.Instance.CheckOutDate + settings.CheckOutDurationLimit,
                        // `ifLimit`, `otherwise` 처럼 plain english 를 쓴다.
                        ifLimited => ifLimited
                            .Then(context =>
                            {
                                // 최대 허용 납기를 넘기지 않게 DueDate 를 조정한다.
                                context.Instance.DueDate =
                                    context.Instance.CheckOutDate + settings.CheckOutDurationLimit;
                            })
                            // 그리고... 적절한 응답 메시지를 보낸다. 
                            .RespondAsync(context => context.Init<CheckOutDurationLimitReached>(new
                            {
                                CheckOutId = context.Instance.CorrelationId,
                                DueDate = context.Instance.DueDate
                            })),
                        otherwise => otherwise
                            // DueDate 가 갱신되었으므로 ...  사용자에게 알린다음... 
                            .Activity(x => x.OfInstanceType<NotifyMemberActivity>())
                            // 응답메시지를 한번 날려보자.
                            .RespondAsync(context => context.Init<CheckOutRenewed>(new
                            {
                                CheckOutId = context.Instance.CorrelationId, // = context.Data.CheckOutId
                                DueDate = context.Instance.DueDate
                            })))
            );
        }
        public State CheckedOut { get; }
        
        public Event<BookCheckedOut> BookCheckedOut { get; }
        public Event<RenewCheckOut> RenewCheckOut { get; }
        
    }

    // Automatonymous Activity 임( != Courier Activity)
}