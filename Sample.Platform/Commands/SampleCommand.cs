using System;

namespace Sample.Platform.Commands
{
    public interface SampleCommand
    {
        Guid CommandId { get; }
        string Command { get; }
    }
}