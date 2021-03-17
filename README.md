학습시 코드에 주석으로 기록하지 못한 메모

# 🍟 Masstransit 조각 메모

- IntelliSense 가 없으면 사용이 힘들다. 😥
- Masstransit는 _acknowledgement mode_ 로 동작 한다. ([여기](https://masstransit-project.com/articles/outbox.html#the-in-memory-outbox) 참고)
- [Outbox 를 사용하여 Resilient System 구성하는 영상](https://www.youtube.com/watch?v=P41IsVAc1nI&list=PLx8uyNNs1ri2MBx6BjPum5j9_MMdIfM9C&index=21) 다시 들여다보자.
- `Automatonymous.Visualizer` 패키지의 `StateMachineGraphvizGenerator` 는 `Automatonymous.Graphing` 네임스페이스의 `GetGraph<TInstance>()` 확장메소드를 통해 Statemachine의 Statechart Diagram을 시각화할 수 있다.

```cs
 public void Show_me_the_StateMachine()
        {
            // 주어진 상태기계를 GraphViz 로 시각화가 가능.
            var orderStateMachine = new OrderStateMachine();
            var graph = orderStateMachine.GetGraph();
            var generator = new StateMachineGraphvizGenerator(graph);
            var dots = generator.CreateDotFile();
            Console.WriteLine(dots);
        }
```

- `MassTransit.Metadata.TypeMetadataCache` 를 사용하면, 단순히 `GetType()` 하는 것 보다 더 많은 정보를 알 수 있다. Message, Consumer, Statemachine 등의 type정보를 접근할 때 활용가능.
- `IBusControl.GetProbeResult()` 는 Consumer, Saga Statemachine, Filter, 등 메시징 파이프라인의 모든 내역을 덤프한다. 따라서, 이 정보를 사용하면, 시스템의 구성을 시각화할 수도 있겠다.
- Greenpipes 의 Payload 개념에 대해서 https://youtu.be/Y4Z5puk4jW4?t=3165 에서 확인.
- MassTransit의 Initializer를 알아두면 좋을것 같다.

  - `object` 형으로 받기 때문에, 속성구성이 동일한 여러 Type들간의 대입이 용이하다. 마치 duck typing 같다. 단점은???? 실수 할 수도 있겠지.... 그래서 Masstransit.Analyzer 같은 lint 도구를 만든거 같다. `@one-reason-to-use-message-initializer` 를 코드에서 검색해 보라. 마치 아래 코드 같은 개념...

    ```cs
    interface MessageA {
      Guid MemberId { get; }
    }
    interface MessageB {
      Guid MemberId { get; }
    }

    void HandleMessagA(MessageA  a)
    {
      // a 를 수신한 측에서..  아래처럼 b 에 a 를 대입..
      _publisher.Publish<MessageB>(a);
    }
    ```

  - `Task<T>` 형 값을 dynamic 객체 속성으로 넘기면, `context.Init<T>()` 에서는 알아서 `await` 해서 `T` 값을 적용한다. (소스코드내 `@masstransit-initializer-async-type-mapping` 검색)

- InMemorySagaRepository 는 Transaction 을 지원하지 않는다????? see https://youtu.be/yfRRqPtqkgM?t=1297
- Saga Repository 는 초기 생성시의 Concurrency 를 잘 생각해야 한다고 한다(Saga를 생성시키는 메시지가 여러개 있고, 이 것들이 만일 동시에 서로 다른 곳에서 처리되는 등.. )
- Saga Repository 가 막 생성되어 Insert시 무슨 문제가 있다고 한다. https://masstransit-project.com/usage/sagas/automatonymous.html#initial-insert . 이것 때문에 아래 코드처럼 한단다.

```cs
       Event(() => BookReserved,
               x =>
               {
                   x.CorrelateBy((state, context) =>
                           state.BookId == context.Message.BookId && context.Message.MemberId == state.MemberId)
                       .SelectId(context => context.MessageId ?? NewId.NextGuid());

                   x.InsertOnInitial = true;  // <--------------- 이거!!!!!!!!!!
               });
```

- Saga StateMachine 에서 Publish 하는것은 OK 지만, Request/Respond 는 가급적 하지 말아야함. (Saga가 해당 메시지 응답을 기다리는 동안 Lock 되며, 이렇게 되면 Saga Pattern의 의미가 축소됨. Saga Repository의 Lock 이 Pessimistic 인 경우에 그러함.)

# 💌 masstransit 이 실제로 보낸 메시지의 형태 예시

Properties(=header)의 내역은..

```ini
message_id:	82d30000-5d21-0015-4a10-08d8d959e63c
expiration:	30000 --> 30 초가 지나면 메시지가 사라짐.
delivery_mode:	2 --> Non-persistent (1) or persistent (2). 이므로... 즉, 여기서는 persistant
headers:
Content-Type:	application/vnd.masstransit+json --> payload 의 스키마
MT-Activity-Id:	|51084beb-4c2f5f5bfee97d51.1.   --> app insight 같은 도구에서 메시지 추적을 할 수 있게 해준다.
publishId:	5
content_type:	application/vnd.masstransit+json
```

payload(=body)의 내역은...

```json
{
  "messageId": "82d30000-5d21-0015-4a10-08d8d959e63c", --> Message 당 고유 ID
  "requestId": "82d30000-5d21-0015-4657-08d8d959e63c", -->
  "conversationId": "82d30000-5d21-0015-770c-08d8d959e63c",
  "sourceAddress": "rabbitmq://localhost/DESKTOPU6RPOO1_SampleApi_bus_omjoyyn7rrybmzb3bdcp1scm8h?temporary=true",
  "destinationAddress": "rabbitmq://localhost/Sample.Contracts:SubmitOrder",
  "responseAddress": "rabbitmq://localhost/DESKTOPU6RPOO1_SampleApi_bus_omjoyyn7rrybmzb3bdcp1scm8h?temporary=true",
  "messageType": ["urn:message:Sample.Contracts:SubmitOrder"],
  "message": {
    "orderId": "e1fab15c-e7a0-47f3-ab06-462433dd65d7",
    "timeStamp": "2021-02-25T06:52:21.0951727Z",
    "customerNumber": "NORMALUSER"
  },
  "expirationTime": "2021-02-25T06:52:51.09704Z", --> header의 expiration이 적용되었을때 실제 expire되는 시점. 아래 sentTime + 30초
  "sentTime":       "2021-02-25T06:52:21.095272Z",
  "headers": {
    "MT-Activity-Id": "|51084beb-4c2f5f5bfee97d51.1." --> header의 내용
  },
  "host": {
    "machineName": "DESKTOP-U6RPOO1",
    "processName": "Sample.Api",
    "processId": 16588,
    "assembly": "Sample.Api",
    "assemblyVersion": "1.0.0.0",
    "frameworkVersion": "3.1.12",
    "massTransitVersion": "6.2.5.0",
    "operatingSystemVersion": "Microsoft Windows NT 10.0.19042.0"
  }
}
```

# Masstransit 이 자체적으로 가지는 규약에 의한 Exchange/Queue 관리

가 있지만,

```cs


            services.AddMassTransit(configurator =>
            {
                //configurator.AddConsumer<SubmitOrderConsumer>();
                //configurator.AddMediator();
                configurator.AddBus(provider =>
                {
                    return Bus.Factory.CreateUsingRabbitMq(sbc =>
                    {
                        sbc.Host("rabbitmq://admin:mirero@localhost:5672");
                        sbc.ConfigureEndpoints(provider, KebabCaseEndpointNameFormatter.Instance);
                    });
                });
                var uriName = KebabCaseEndpointNameFormatter.Instance.Consumer<SubmitOrderConsumer>();
                _logger.LogInformation($"UriName : {uriName}");
                configurator.AddRequestClient<SubmitOrder>(new Uri($"exchange:{uriName}"),TimeSpan.FromDays(3));
            });
            services.AddMassTransitHostedService();
```

처럼 "exchange:uriName" 을 주면 마치, raw rabbitmq client 라이브러리로 했을때 처럼 할 수 있단다.
물론 이경우, consuming 하는 쪽 서비스가 반드시 먼저 실행되어 Exchange/Queue 연결의 Topology를 먼저 구성한 다음 Publisher가 실행되어야 한단다.
