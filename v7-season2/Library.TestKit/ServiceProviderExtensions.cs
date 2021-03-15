using System;
using Automatonymous;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Library.TestKit
{
    public static class ServiceProviderExtensions
    {
        // public static IStateMachineSagaTestHarness<TInstance, TStateMachine>
        //     GetRequiredSaga<TStateMachine, TInstance>(this IServiceProvider me)
        //     where TStateMachine : SagaStateMachine<TInstance>
        //     where TInstance : class, SagaStateMachineInstance
        // {
        //     return me.GetRequiredService<IStateMachineSagaTestHarness<TInstance, TStateMachine>>();
        // }
    }
}