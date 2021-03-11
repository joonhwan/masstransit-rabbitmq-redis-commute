using System;

namespace LongRun.Contracts
{
    public interface DoIt
    {
        string Command { get; }
        TimeSpan Duration { get; }
    }
}