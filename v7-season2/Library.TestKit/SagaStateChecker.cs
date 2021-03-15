using System;
using System.Threading.Tasks;
using Automatonymous;
using MassTransit.Testing;

namespace Library.TestKit
{
    /*
         public interface IStateMachineSagaTestHarness<TInstance, out TStateMachine> :
            ISagaTestHarness<TInstance>
            where TInstance : class, SagaStateMachineInstance
            where TStateMachine : SagaStateMachine<TInstance>
        {
     */
    public class SagaStateChecker<TInstance, TStateMachine>
        where TInstance : class, SagaStateMachineInstance
        where TStateMachine : SagaStateMachine<TInstance>
    {
        private readonly IStateMachineSagaTestHarness<TInstance, TStateMachine> _harness;
        private readonly Guid _correlationId;

        public SagaStateChecker(IStateMachineSagaTestHarness<TInstance, TStateMachine> harness, Guid correlationId)
        {
            _harness = harness;
            _correlationId = correlationId;
        }

        public async Task<bool> ExistsAs(Func<TStateMachine, State> stateSelector)
        {
            var id = await _harness.Exists(_correlationId, machine => stateSelector?.Invoke(machine));
            return id.HasValue;
        }
        
        public async Task<bool> Exists()
        {
            var id = await _harness.Exists(_correlationId);
            return id.HasValue;
        }
        
        public async Task<bool> NotExists()
        {
            var id = await _harness.NotExists(_correlationId);
            return !id.HasValue;
        }
    }
    
    public static class SagaHarnessExtensions
    {
        public static SagaStateChecker<TInstance, TStateMachine> SagaOf<TInstance, TStateMachine>(
            this IStateMachineSagaTestHarness<TInstance, TStateMachine> harness, Guid correlationId)
            where TInstance : class, SagaStateMachineInstance
            where TStateMachine : SagaStateMachine<TInstance>
        {
            return new SagaStateChecker<TInstance, TStateMachine>(harness, correlationId);
        }
    }
}