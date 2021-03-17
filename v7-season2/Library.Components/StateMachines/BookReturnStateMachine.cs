using System;
using System.Numerics;
using Automatonymous;
using Library.Components.Consumers;
using Library.Contracts.Messages;
using MassTransit;

namespace Library.Components.StateMachines
{
    public class BookReturnStateMachine : MassTransitStateMachine<BookReturnSaga>
    {
        public BookReturnStateMachine(IEndpointNameFormatter namer)
        {
            Event(() => BookReturned, x => x.CorrelateById(m => m.Message.CheckOutId));
            
            Request(() => ChargingMemberFineRequest, x => x.FineRequestId,
                x =>
                {
                    // var endpoint = namer.Consumer<ChargeMemberFineConsumer>();
                    // x.ServiceAddress = new Uri($"queue:{endpoint}");
                    x.Timeout = TimeSpan.FromSeconds(10);
                });
            
            InstanceState(x => x.CurrentState);

            Initially(
                When(BookReturned)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.CheckOutAt = context.Data.Timestamp;
                        context.Instance.ReturnedAt = context.Data.ReturnedAt;
                        context.Instance.DueDate = context.Data.DueDate;
                    })
                    .IfElse(context => context.Data.ReturnedAt > context.Instance.DueDate,
                        _ => _
                            .Request(ChargingMemberFineRequest,
                                context => context.Init<ChargeMemberFine>(new
                                {
                                    MemberId = context.Data.MemberId,
                                    Amount = 123.45m,
                                }))
                            .TransitionTo(ChargingInProgress),
                        _ => _
                            .TransitionTo(Complete)
                    )
            );

            During(ChargingInProgress,
                When(ChargingMemberFineRequest.Completed) // FineCharged 가 수신된 경우.
                    .TransitionTo(Complete),
                When(ChargingMemberFineRequest.Completed2) // FineOverriden 이 수신된 경우
                    .TransitionTo(Complete),
                When(ChargingMemberFineRequest.Faulted)
                    .TransitionTo(ChargingFailed),
                When(ChargingMemberFineRequest.TimeoutExpired)
                    .TransitionTo(ChargingFailed)
            );
        }
        
        public Event<BookReturned> BookReturned { get; }
        
        public State ChargingInProgress { get; }
        public State ChargingFailed { get; }
        public State Complete { get; }

        //public Request<BookReturnSaga, ChargeMemberFine, FineCharged> ChargingMemberFineRequest { get; }
        // Automatonymous 의 Request 는 2개까지의 응답메시지를 정할 수 있다.  위 응답 메시지 처리 부분의 Completed2 확인.
        public Request<BookReturnSaga, ChargeMemberFine, FineCharged, FineOverriden> ChargingMemberFineRequest { get; }
    }
}