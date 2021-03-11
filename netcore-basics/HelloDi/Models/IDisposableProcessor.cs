using System;

namespace HelloDi.Models
{
    public interface IDisposableProcessor : IDisposable
    {
        void Process(string data);
    }
}