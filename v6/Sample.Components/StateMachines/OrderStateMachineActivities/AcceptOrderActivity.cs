using System;
using System.Threading.Tasks;
using Automatonymous;
using GreenPipes;
using MassTransit;
using MassTransit.Courier.Contracts;
using Sample.Contracts;

namespace Sample.Components.StateMachines.OrderStateMachineActivities
{
    // 상태기계가 수행할 내역은 모두 "Activity" 라고 한다. 통산 기존 Activity클래스들로도 충분하지만,
    // 커스텀 Activity 도 만들 수 있다. 이렇게 하면, 특정 행위를 수행할때, 필요한 DB 접속, Web 서비스 접근 등 
    // External System을 위한 여러 의존성을 State Machine 으로 부터 Loose Coupling 할 수 있다고 한다.
    //
    // - Automatonymous.Activities.ActionActivity (가장 기본이 되는 Activity)
    // - Automatonymous.Activities.TransitionActivity,
    // - ...
    // 같은 기존 구현을 참고하면 좋다. 
    // 
    // Custom Activity 에 대한 문서는 https://masstransit-project.com/usage/sagas/automatonymous.html#custom
    // (NOTE: Automatanous 의 Activity 는 Masstrransit.Courier의 Activity 와 다른것.
    //
    // OrderState 를 관리하는 상태기계에서 OrderAccepted 메시지에 의해 실행되는 활동임.
    // --->  Activity<OrderState, OrderAccepted> 
    public class AcceptOrderActivity : Activity<OrderState, OrderAccepted> 
    {
        public void Probe(ProbeContext context)
        {
            // 먼가 상태기계의 정보를 시각화하는거 하고 관련이 있단다.
            context.CreateScope("accept-order");
        }

        public void Accept(StateMachineVisitor visitor)
        {
            visitor.Visit(this);
        }

        public async Task Execute(BehaviorContext<OrderState, OrderAccepted> context, Behavior<OrderState, OrderAccepted> next)
        {
            // TODO  Do something here...
            Console.WriteLine("AcceptOrderActivity 가 작업합니다😁. Event = {0}, OrderId = {1}", context.Event, context.Data.OrderId);

            var consumeContext = context.GetPayload<ConsumeContext>();
            var sendEndpoint = await consumeContext.GetSendEndpoint(new Uri("exchange:fulfill-order"));
            await sendEndpoint.Send<FulfillOrder>(new
            {
                // Automatonymous 의 Activity는 ....
                //
                // 이 Activity 를 실행하게끔 만든 Event 의 데이터에 접근이 가능.
                OrderId = context.Data.OrderId,
                // 이 Activity 를 수행하고 있는 StateMachine의 상태정보에 접근이 가능.
                PaymentCardNumber = context.Instance.PaymentCardNumber,
                CustomerNumber = context.Instance.CustomerNumber,
            });
            
            // Middleware! 이니까... next() 를 수행해...
            await next.Execute(context).ConfigureAwait(false);
        }

        public async Task Faulted<TException>(BehaviorExceptionContext<OrderState, OrderAccepted, TException> context, Behavior<OrderState, OrderAccepted> next) where TException : Exception
        {
            // 아래쪽? 에서 오류가 났을때...해야 할 일을 여기서???
            Console.WriteLine("AcceptOrderActivity 가 먼가오류를 처리했습니다!.");
            
            await next.Faulted(context);
        }
    }
}