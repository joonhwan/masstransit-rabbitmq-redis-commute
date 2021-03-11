using System;

namespace HelloDi.Models
{
    internal class DisposableProcessor : IDisposableProcessor
    {
        public DisposableProcessor()
        {
            Console.WriteLine("DisposableProcessor : Created!");
        }
        
        public void Dispose()
        {
            Console.WriteLine("DisposableProcessor : Disposed!");
        }

        public void Process(string data)
        {
            Console.WriteLine("DisposableProcessor : Process( {0} )", data);
        }
    }
}