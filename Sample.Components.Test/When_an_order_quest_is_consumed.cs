using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;
using Sample.Contracts;

namespace Sample.Components.Test
{
    [TestFixture]
    public class When_an_order_quest_is_consumed
    {
        
        [Test]
        public async Task Should_consume_submit_order_command()
        {
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer<SubmitOrderConsumer>();
            
            await harness.Start();
            try
            {
                var orderId = NewId.NextGuid();

                // 메시지를 Publish 하고, Response가 필요없는 경우, 그냥 직접 InputQueue 에 메시지를 전송해볼 수 있다.
                // --> 그러면, 정상적인 경우? 라면 등록된 Consumer 가 메시지를 수신하겠지?
                await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = "12345",
                });

                Assert.That(consumer.Consumed.Select<SubmitOrder>().Any(), Is.True);
            }
            finally
            {
                await harness.Stop();
            }
        }
        
        [Test]
        public async Task Should_consume_submit_order_command_not_not_send_OrderSubmissionAccepted_and_OrderSubmissionRejected()
        {
            // 이 테스트는 "어떤 메시지는 절대 오지 않아"를 테스트 하므로, Timeout 문제가 발생한다. 
            // 따라서, 테스트를 빨리 진행하기 위해, timeout 을 지정해 준다.
            var harness = new InMemoryTestHarness()
            {
                TestTimeout = TimeSpan.FromSeconds(5)
            };
            
            var consumer = harness.Consumer<SubmitOrderConsumer>();
            
            await harness.Start();
            try
            {
                var orderId = NewId.NextGuid();

                // 메시지를 Publish 하고, Response가 필요없는 경우, 그냥 직접 InputQueue 에 메시지를 전송해볼 수 있다.
                // --> 그러면, 정상적인 경우? 라면 등록된 Consumer 가 메시지를 수신하겠지?
                await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = "12345",
                });

                Assert.That(consumer.Consumed.Select<SubmitOrder>().Any(), Is.True);

                // Response를 기대하고 전송하지 않았기 때문에(즉, requestClient.GetResponse() 를 사용하지 않았기 때문에)
                // Consumer는 응답메시지를 전송하지 않는다. 따라서 아래 테스트는 통과 된다.
                Assert.That(harness.Sent.Select<OrderSubmissionAccepted>().Any(), Is.False); // Timeout 5초 
                Assert.That(harness.Sent.Select<OrderSubmissionRejected>().Any(), Is.False); // Timeout 5초 --> 총 10초 대기
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task Should_publish_OrderSubmitted_event()
        {
            // 이 테스트는 "어떤 메시지는 절대 오지 않아"를 테스트 하므로, Timeout 문제가 발생한다. 
            // 따라서, 테스트를 빨리 진행하기 위해, timeout 을 지정해 준다.
            var harness = new InMemoryTestHarness()
            {
                TestTimeout = TimeSpan.FromSeconds(5)
            };
            
            var consumer = harness.Consumer<SubmitOrderConsumer>();
            
            await harness.Start();
            try
            {
                var orderId = NewId.NextGuid();

                // 메시지를 Publish 하고, Response가 필요없는 경우, 그냥 직접 InputQueue 에 메시지를 전송해볼 수 있다.
                // --> 그러면, 정상적인 경우? 라면 등록된 Consumer 가 메시지를 수신하겠지?
                await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = "12345",
                });

                // consumer가 메시지 수신하고...
                Assert.That(consumer.Consumed.Select<SubmitOrder>().Any(), Is.True);
                // OrderSubmitted 가 Publish 된다..를 테스트
                Assert.That(harness.Published.Select<OrderSubmitted>().Any(), Is.True); 
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task Should_not_publish_OrderSubmitted_event_if_test_user()
        {
            // 이 테스트는 "어떤 메시지는 절대 오지 않아"를 테스트 하므로, Timeout 문제가 발생한다. 
            // 따라서, 테스트를 빨리 진행하기 위해, timeout 을 지정해 준다.
            var harness = new InMemoryTestHarness()
            {
                TestTimeout = TimeSpan.FromSeconds(5)
            };
            
            var consumer = harness.Consumer<SubmitOrderConsumer>();
            
            await harness.Start();
            try
            {
                var orderId = NewId.NextGuid();

                // 메시지를 Publish 하고, Response가 필요없는 경우, 그냥 직접 InputQueue 에 메시지를 전송해볼 수 있다.
                // --> 그러면, 정상적인 경우? 라면 등록된 Consumer 가 메시지를 수신하겠지?
                await harness.InputQueueSendEndpoint.Send<SubmitOrder>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = "TEST",
                });

                // Consumer 가 SubmitOrder 를 수신하고 처리했지만...
                Assert.That(consumer.Consumed.Select<SubmitOrder>().Any(), Is.True);
                // OrderSubmitted 메시지는 Publish 되지 않았음을 테스트.
                Assert.That(harness.Published.Select<OrderSubmitted>().Any(), Is.False); 
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Test]
        public async Task Should_response_with_acceptance_if_ok()
        {
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer<SubmitOrderConsumer>();
            
            await harness.Start();
            try
            {
                var orderId = NewId.NextGuid();
                
                var requestClient = await harness.ConnectRequestClient<SubmitOrder>();
                var response = await requestClient.GetResponse<OrderSubmissionAccepted>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = "12345",
                });

                Assert.That(response.Message.OrderId, Is.EqualTo(orderId));
                Assert.That(consumer.Consumed.Select<SubmitOrder>().Any(), Is.True);
                Assert.That(harness.Sent.Select<OrderSubmissionAccepted>().Any(), Is.True);
            }
            finally
            {
                await harness.Stop();
            }
        }
        
        
        [Test]
        [Ignore("테스트 하면 오래걸린다. requestClient.GetResponse() 가 원하는 메시지를 못받으면, timeout (아마 30초)될 때까지 대기")]
        public async Task Should_response_with_accept_if_test_user_DO_NOT_TEST_THIS()
        {
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer<SubmitOrderConsumer>();
            
            await harness.Start();
            try
            {
                var orderId = NewId.NextGuid();
                
                var requestClient = await harness.ConnectRequestClient<SubmitOrder>();
                var response = await requestClient.GetResponse<OrderSubmissionAccepted>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = "TEST",
                });

                Assert.That(response.Message.OrderId, Is.EqualTo(orderId));
                Assert.That(consumer.Consumed.Select<SubmitOrder>().Any(), Is.True);
                // 시스템이 OrderSubmissionRejected 를 전송했는지 확인.
                Assert.That(harness.Sent.Select<OrderSubmissionAccepted>().Any(), Is.True);
            }
            finally
            {
                await harness.Stop();
            }
        }
        
        [Test]
        public async Task Should_response_with_reject_if_test_user()
        {
            var harness = new InMemoryTestHarness();
            var consumer = harness.Consumer<SubmitOrderConsumer>();
            
            await harness.Start();
            try
            {
                var orderId = NewId.NextGuid();
                
                var requestClient = await harness.ConnectRequestClient<SubmitOrder>();
                var response = await requestClient.GetResponse<OrderSubmissionRejected>(new
                {
                    OrderId = orderId,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = "TEST",
                });

                Assert.That(response.Message.OrderId, Is.EqualTo(orderId));
                Assert.That(consumer.Consumed.Select<SubmitOrder>().Any(), Is.True);
                // 시스템이 OrderSubmissionRejected 를 전송했는지 확인.
                Assert.That(harness.Sent.Select<OrderSubmissionRejected>().Any(), Is.True);
            }
            finally
            {
                await harness.Stop();
            }
        }

    }
}