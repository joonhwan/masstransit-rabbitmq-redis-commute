using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Sample.Components.StateMachines;
using Sample.Contracts;

namespace Sample.Components.Test
{
    [TestFixture]
    public class OrderStateMachine_Specs
    {
        [Test]
        public async Task Should_create_a_state_instance()
        {
            var harness = new InMemoryTestHarness()
            {
                TestTimeout = TimeSpan.FromSeconds(1)
            };
            var orderStateMachine = new OrderStateMachine(); 
            var saga = harness.StateMachineSaga<OrderState, OrderStateMachine>(orderStateMachine);
            
            await harness.Start();

            var orderId = NewId.NextGuid();
            var customerNumber = "1234";

            try
            {
                // 아래는 "없는 걸" 테스트 하므로, Timeout 문제 발생함. 따라서 test harness에 timeout 을 주었다. 
                Assert.That(saga.Created.Select(x => x.CorrelationId == orderId).Any(), Is.False);

                await harness.Bus.Publish<OrderSubmitted>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = customerNumber
                });

                Assert.That(saga.Created.Select(x => x.CorrelationId == orderId).Any(), Is.True);

                
                // 비동기 코드에 대한 테스트는 항상 Race Condition이 있을 수 있다. 
                // 으래 코드는 `Task.Delay()` 가 없으면 실패한다. (테스트 코드가 실행되는 속도가 메시지 처리되는 속도 보다 빠르기 때문)
                // await Task.Delay(TimeSpan.FromSeconds(1));
                // var instance = saga.Created.Contains(orderId);
                // Assert.That(instance, Is.Not.Null);
                // Assert.That(instance.CurrentState, Is.EqualTo(orderStateMachine.Submitted.Name));
                
                // 따라서, Masstransit Test Harness Saga 는 "특정 saga 가 특정 상태가 되기를 대기 하게 할 수 있다.
                // orderId 의 correlation id 를 가지는 saga 가 submitted 상태에 있을 때 가지 대기
                var instanceId = await saga.Exists(orderId, x => x.Submitted, timeout: null);  // timeout 을 null 로 주면, Test harness의 TestTimeout 값이 사용됨.
                Assert.That(instanceId, Is.Not.Null);

                var instance = saga.Sagas.Contains(instanceId.Value);
                Assert.That(instance.CustomerNumber, Is.EqualTo(customerNumber));
            }
            finally
            {
                await harness.Stop();
            }
        }

                [Test]
        public async Task Should_respond_to_status_check()
        {
            var harness = new InMemoryTestHarness()
            {
                TestTimeout = TimeSpan.FromSeconds(1)
            };
            var orderStateMachine = new OrderStateMachine(); 
            var saga = harness.StateMachineSaga<OrderState, OrderStateMachine>(orderStateMachine);
            
            await harness.Start();

            var orderId = NewId.NextGuid();
            var customerNumber = "1234";

            try
            {
                // 아래는 "없는 걸" 테스트 하므로, Timeout 문제 발생함. 따라서 test harness에 timeout 을 주었다. 
                Assert.That(saga.Created.Select(x => x.CorrelationId == orderId).Any(), Is.False);

                await harness.Bus.Publish<OrderSubmitted>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = customerNumber
                });

                Assert.That(saga.Created.Select(x => x.CorrelationId == orderId).Any(), Is.True);

                var requestClient = await harness.ConnectRequestClient<CheckOrder>();
                var response = await requestClient.GetResponse<OrderStatus>(new
                {
                    OrderId = orderId
                });
                Assert.That(response.Message.State, Is.EqualTo(orderStateMachine.Submitted.Name));
            }
            finally
            {
                await harness.Stop();
            }
        }

    }
}