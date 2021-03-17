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

- Saga StateMachine 에서 Publish 하는것은 OK 지만, Request/Respond 는 가급적 하지 말아야함. (Saga가 해당 메시지 응답을 기다리는 동안 Block되고 Repository는 Lock 되며, 이렇게 되면 Saga Pattern의 의미가 축소됨. Saga Repository의 Lock 이 Pessimistic 인 경우에 그러함.)
- ---> 그럼에도 Reqest/Respond 를 써야 한다면, https://masstransit-project.com/usage/sagas/automatonymous.html#request 참고. 아래 `ChargingMemberFineRequest` 예시도 참고. (Request/Response 대신, Publish + 상태관리로도 얼마든지 할 수 있다. ... 그런데, v7.1 이 되면서 Request 기능에도 발전이 있었다고는 한다.)

  ```cs
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
                When(ChargingMemberFineRequest.Completed)
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

        public Request<BookReturnSaga, ChargeMemberFine, FineCharged> ChargingMemberFineRequest { get; }
    }
  ```

- `IConsumer<T>` 파생 클래스에서는 `Respond()` 도 할 수 있다!

  ```cs
  public class ChargingMemberFineConsumer : IConsumer<ChargingMemberFine>
   {
       public async Task Consume(ConsumeContext<ChargingMemberFine> context)
       {
           await Task.Delay(1000);

           // Consumer는 ....
           //
           // - context.Publish<T>()
           // - context.Send<T>
           // ... 에 다가.. 받은 메시지에 응답까지 할 수 있음.
           await context.RespondAsync<FineCharged>(context.Message);
       }
   }
  }
  ```

- Unit Test 시 Message의 Type정보만으로 불충분하다면, message id 로 콕 집어서 그 메시지를 수신했냐 안했냐...를 아래처럼 확인가능.

  ```cs
      var messageId = NewId.NextGuid();

      await TestHarness.Bus.Publish<BookReturned>(new
      {
          CheckOutId = checkOutId,
          Timestamp = InVar.Timestamp,
          BookId = bookId,
          MemberId = memberId,
          DueDate = dueDate,
          ReturnedAt = returnedAt,
          __MessageId = messageId
      });

      // 이렇게 해도 되네.. 콕 찝어서 딱 그 메시지! 라고 하려면, message id 를 사용해야 겠다.
      Assert.IsTrue(await TestHarness.Consumed.Any<BookReturned>(x => x.Context.MessageId == messageId));
  ```

- Consumer 등록시, 특정 Endpoint 를 지정하려면 `cfg.AddConsumer<SubmitOrderConsumer>().EndPoint(x => x.Name = "submit-order")` 이런식으로 할 수 _도_ 있다.
<<<<<<< HEAD

- Request Timeout 관련해서 .... Quartz Scheduler만 Schedule 취소가 가능하다고 한다. RabbitMQ 는 요청 취소가 안된다. Azure Service는 또 된다고 한다.

- v7.x 부터 Bus 는.. Start/Stop/Start/Stop ... 을 계속 반복할 수 있다(이전에는 불가능.) . 서비스를 재기동하지 않고도 새로운 메시지 or Topology 를 적용가능????

- v7.x 부터 Consumer는 `context.IsResponseAccepted<TResponseMessage>()` 로 헤더에 포함된 응답메시지 유형정보를 사용하여 요청측이 해당 타입의 응답메시지를 받을 준비가 되었는지 확인 가능. --> 시스템 전체가 시간에 따라 기능이 분화되는 경우, Backward compatibility 를 부여할 수 있다.
=======

- Request Timeout 관련해서 .... Quartz Scheduler만 Schedule 취소가 가능하다고 한다. RabbitMQ 는 요청 취소가 안된다. Azure Service는 또 된다고 한다.
>>>>>>> dbb60ee0f706ef450925d5449fe306c1a04b1415

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
