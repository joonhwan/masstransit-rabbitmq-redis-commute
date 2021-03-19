using MassTransit;
using MassTransit.Courier;
using MassTransit.Definition;
using MassTransit.Saga;

namespace CommuteSystem
{
    /// <summary>
    ///  Masstransit의 Message, Consumer, Saga, Temporary Endpoint들에 대하여 명칭을 붙여 구분하기 쉽게 Naming 한다.
    /// </summary>
    internal class CustomEndpointNameFormatter : IEndpointNameFormatter
    {
        private readonly DefaultEndpointNameFormatter _impl = new DefaultEndpointNameFormatter(false);
        
        public string TemporaryEndpoint(string tag)
        {
            return $"Temporary.{_impl.TemporaryEndpoint(tag)}";
        }

        public string Consumer<T>() where T : class, IConsumer
        {
            return $"Consumer.{_impl.Consumer<T>()}";
        }

        public string Message<T>() where T : class
        {
            var result = $"Message.{_impl.Message<T>()}";
            return result;
        }

        public string Saga<T>() where T : class, ISaga
        {
            return $"Saga.{_impl.Saga<T>()}";
        }

        public string ExecuteActivity<T, TArguments>() where T : class, IExecuteActivity<TArguments> where TArguments : class
        {
            return _impl.ExecuteActivity<T, TArguments>();
        }

        public string CompensateActivity<T, TLog>() where T : class, ICompensateActivity<TLog> where TLog : class
        {
            return _impl.CompensateActivity<T, TLog>();
        }

        public string SanitizeName(string name)
        {
            return _impl.SanitizeName(name);
        }
    }
}