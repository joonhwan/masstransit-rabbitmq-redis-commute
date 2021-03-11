// 이 예제 코드에서 명시적으로 namespace 를 이렇게 지정한 이유는,
// namespace 가 exchange 명칭에 포함되기 때문. 

using System;
using System.Threading.Tasks;
using MassTransit;
using Sample.Contracts;

namespace Sample.Contracts 
{
    public interface UpdateAccount
    {
        string AccountNumber { get; }
    }

    public interface DeleteAccount
    {
        string AccountNumber { get;  }
    }
}

namespace Sample.Components
{
    public class UpdateAccountSubscriber : IConsumer<UpdateAccount>
    {
        public Task Consume(ConsumeContext<UpdateAccount> context)
        {
            Console.WriteLine("알림 : Update Account 작업 처리 요청. : AccountNumber = {0}", context.Message.AccountNumber);
            return Task.CompletedTask;
        }
    }

    public class UpdateAccountConsumer : IConsumer<UpdateAccount>
    {
        public async Task Consume(ConsumeContext<UpdateAccount> context)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            Console.WriteLine("Update Account 작업 처리됨 : AccountNumber = {0} on {1}"
                , context.Message.AccountNumber, context.ReceiveContext.InputAddress);
        }
    }
    public class AnotherUpdateAccountConsumer : IConsumer<UpdateAccount>
    {
        public async Task Consume(ConsumeContext<UpdateAccount> context)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            Console.WriteLine("Another !!! Update Account 작업 처리됨 : AccountNumber = {0}", context.Message.AccountNumber);
        }
    }

    public class DeleteAccountConsumer : IConsumer<DeleteAccount>
    {
        public Task Consume(ConsumeContext<DeleteAccount> context)
        {
            Console.WriteLine("Delete Account 작업 처리됨 : AccountNumber = {0}", context.Message.AccountNumber);
            return Task.CompletedTask;
        }
    }
}