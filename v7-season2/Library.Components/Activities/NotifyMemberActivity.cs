using System;
using System.Threading.Tasks;
using Automatonymous;
using GreenPipes;
using Library.Components.Services;
using Library.Components.StateMachines;
using Library.Contracts.Messages;
using MassTransit;

namespace Library.Components.Activities
{
    public class NotifyMemberActivity : SimpleSagaActivity<CheckOutSaga> //Automatonymous.Activity<CheckOutSaga>
    {
        private readonly ConsumeContext _consumeContext;
        private readonly IMemberRegistry _memberRegistry;

        // ConsumeContext<T> 같이 Activity 에서 사용할 만한 것은 DI가 기본 제공된다.
        // 단, 이 Activity 를 생성하는 쪽에서 DI Container를 가지고 있어야 한다.
        // --> 예 ...   binder.Activity(x => x.OfInstanceType<NotifyMemberActivity>())
        //              -->  ContainerFactoryActivity로 감싼 Activity안에서 만들어지고, DI Container가 동작한다. 
        public NotifyMemberActivity(ConsumeContext consumeContext, IMemberRegistry memberRegistry)
        {
            _consumeContext = consumeContext;
            _memberRegistry = memberRegistry;
        }
        
        // 실제 이 Activity 에서의 수행내용.
        public override void Probe(ProbeContext context)
        {
            context.CreateScope("notifyMember"); // 상태기계 시각화 모듈에 표시된다. 
        }

        protected override async Task Execute(BehaviorContext<CheckOutSaga> context)
        {
            var isValid = await _memberRegistry.IsMemberValid(context.Instance.MemberId);
            if (!isValid)
            {
                throw new InvalidOperationException($"Invalid Member(MemberId = {context.Instance.MemberId})");
            }
            
            //------------- Publish 를 위해 필요한 ConsumeContext 를 얻는 방법 1. 
            //              (GetPayload() 사용)
            // var consumeContext = context.GetPayload<ConsumeContext>();
            // await consumeContext.Publish<NotifyMemberDueDate>(new
            // {
            //     context.Instance.MemberId,
            //     context.Instance.DueDate
            // });
            
            //------------- Publish 를 위해 필요한 ConsumeContext 를 얻는 방법 2. 
            //              (DI Container)
            await _consumeContext.Publish<NotifyMemberDueDate>(new
            {
                context.Instance.MemberId,
                context.Instance.DueDate
            });
        
            await Task.Delay(1000);
        }
    }
}