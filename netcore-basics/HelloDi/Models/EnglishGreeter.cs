using System;

namespace HelloDi.Models
{
    internal class EnglishGreeter : IGreeter
    {
        public void Greet()
        {
            Console.WriteLine("Hello");
        }
    }
}