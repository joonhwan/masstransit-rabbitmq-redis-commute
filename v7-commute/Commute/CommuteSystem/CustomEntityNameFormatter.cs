using MassTransit.Topology;

namespace CommuteSystem
{
    /// <summary>
    /// 임의 T 형 메시지에 대하여 RabbitMQ 상 Message End Point(=RabbitMQ Exchange)의 명칭을 지정.
    /// </summary>
    /// <remarks>
    ///   services.AddMassTransit(x =>
    ///   {
    ///      ...
    ///      x.UsingRabbitMq((context, configurator) =>
    ///      {
    ///           ...
    ///          configurator.SetEndpointNameFormatter(new CustomEndpointNameFormatter());
    ///          ... 
    ///  
    /// 와 같이 사용.
    ///</remarks>
    
    public class CustomEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>()
        {
            return $"Mirero.{typeof(T).Name}";
        }
    }}