using System;

namespace CommuteSystem.Contracts
{
    public interface UpdateCache
    {
        Guid ProductId { get; }
    }

    public interface CacheUpdated
    {
        Guid ProductId { get; }
    }
}