using System;

namespace HelloDi.Models
{
    public class EventProcessor : IEventProcessor
    {
        public EventProcessor()
        {
            Console.WriteLine("EventProcessor : 생성되었습니다");
        }
        
        public void Process(string @event)
        {
            Console.WriteLine("Processed Event[ {0} ]", @event);
        }
    }
}