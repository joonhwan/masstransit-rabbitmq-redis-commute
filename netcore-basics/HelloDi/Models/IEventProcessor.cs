namespace HelloDi.Models
{
    public interface IEventProcessor
    {
        void Process(string @event);
    }
}