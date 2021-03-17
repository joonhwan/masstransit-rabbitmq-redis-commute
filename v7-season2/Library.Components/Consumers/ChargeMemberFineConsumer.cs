using System.Threading.Tasks;
using Library.Components.Services;
using Library.Contracts.Messages;
using MassTransit;

namespace Library.Components.Consumers
{
    public class ChargeMemberFineConsumer : IConsumer<ChargeMemberFine>
    {
        private readonly IFineCharger _fineCharger;

        public ChargeMemberFineConsumer(IFineCharger fineCharger)
        {
            _fineCharger = fineCharger;
        }
        
        public async Task Consume(ConsumeContext<ChargeMemberFine> context)
        {
            var result = await _fineCharger.Charge(context.Message.MemberId, context.Message.Amount);
            
            // Consumer는 ....
            //
            // - context.Publish<T>() 
            // - context.Send<T>
            // ... 에 다가.. 받은 메시지에 응답까지 할 수 있음.
            
            // context.IsResponseAccepted<T>() 는 Masstransit v7.x 에 추가됨.
            // --> 메시지 헤더의 Request.Accepts (응답메시지 타입에 대한 문자열 목록) 으로 부터 확인한다.
            if (result == ChargeResult.Overriden && context.IsResponseAccepted<FineOverriden>())
            {
                await context.RespondAsync<FineOverriden>(context.Message);
                return;
            }
            await context.RespondAsync<FineCharged>(context.Message);
        }
    }
}