using System;
using System.Threading.Tasks;
using Automatonymous;
using GreenPipes;

namespace Library.Components.Activities
{
    public abstract class SimpleSagaActivity<TInstance> : Automatonymous.Activity<TInstance>
    {
        public abstract void Probe(ProbeContext context);
        
        public void Accept(StateMachineVisitor visitor)
        {
            visitor.Visit(this);
        }

        public async Task Execute(BehaviorContext<TInstance> context, Behavior<TInstance> next)
        {
            await Execute(context);
            await next.Execute(context);
        }

        public async Task Execute<T>(BehaviorContext<TInstance, T> context, Behavior<TInstance, T> next)
        {
            await Execute(context);
            await next.Execute(context);
        }

        // 실제 이 Activity 에서의 수행내용.
        protected abstract Task Execute(BehaviorContext<TInstance> context);

        public Task Faulted<TException>(BehaviorExceptionContext<TInstance, TException> context, Behavior<TInstance> next) where TException : Exception
        {
            return next.Faulted(context);
        }

        public Task Faulted<T, TException>(BehaviorExceptionContext<TInstance, T, TException> context, Behavior<TInstance, T> next) where TException : Exception
        {
            return next.Faulted(context);
        }
    }
}