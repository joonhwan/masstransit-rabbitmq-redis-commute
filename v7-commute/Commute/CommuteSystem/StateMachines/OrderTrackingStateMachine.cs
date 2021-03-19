using System;
using Automatonymous;
using CommuteSystem.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CommuteSystem.StateMachines
{
    public class OrderTrackingStateMachine : MassTransitStateMachine<OrderTrackingSaga>
    {
        private readonly ILogger<OrderTrackingStateMachine> _logger;

        public OrderTrackingStateMachine(ILogger<OrderTrackingStateMachine> logger)
        {
            _logger = logger;
            // OrderId 가 CorrelationId 가 됨. 
            Event(() => Submitted, e => e.CorrelateById(m => m.Message.OrderId));
            Event(() => Audited, e => e.CorrelateById(m => m.Message.OrderId));
            //
            Event(() => CacheUpdated, e => e.CorrelateBy((saga, context) => saga.ProductId == context.Message.ProductId));

            InstanceState(x => x.State);

            Initially(
                When(Submitted)
                    .Then(context =>
                    {
                        logger.LogInformation("주문이 접수됨. Tracking 시작.");
                        var message = context.Data;
                        context.Instance.ProductId = message.ProductId;
                        context.Instance.CustomerId = message.CustomerId;
                    })
                    .PublishAsync(c => c.Init<AuditOrder>(new
                    {
                        OrderId = c.Data.OrderId // 수신된 Event(=`OrderSubmitted` type) 에서 값을 가져옴.
                    }))
                    .TransitionTo(Auditing)
            );
            
            During(Auditing,
                When(Audited)
                    .PublishAsync(c => c.Init<UpdateCache>(new
                    {
                        ProductId = c.Instance.ProductId // 저장된 Saga(=`OrderTrackingSaga` type) 에서 값을 가져옴. 
                    }))
                    .TransitionTo(Caching)
            );
            
            During(Caching,
                When(CacheUpdated)
                    .Then(context =>
                    {
                        logger.LogInformation("모든 작업이 완료되었음. Tracking 종료");
                    })
                    .Finalize() // 데모에서는 여기까지 하고 상태머신 종료 (--> `Final` 상태가 됨)
                );

        }
        
        public State Auditing { get; }
        public State Caching { get; }
        // public State 배송중 { get; }
        // public State 배송완료 { get;  }
        // public State 주문취소 { get;  }
        
        public Event<OrderSubmitted> Submitted { get; }
        public Event<CacheUpdated> CacheUpdated { get; }
        public Event<OrderAudited> Audited { get; }
    }

    public class OrderTrackingSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } // OrderId
        public string State { get; set; }
        
        public Guid ProductId { get; set; }
        public Guid CustomerId { get; set; }
        public bool Cached { get; set; }
        public bool Audited { get; set; }
    }
}