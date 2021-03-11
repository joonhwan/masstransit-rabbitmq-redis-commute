using System;

namespace HelloDi.Models
{
    internal class KoreanGreeter : IGreeter
    {
        public void Greet()
        {
            Console.WriteLine("안녕");
        }
    }
}