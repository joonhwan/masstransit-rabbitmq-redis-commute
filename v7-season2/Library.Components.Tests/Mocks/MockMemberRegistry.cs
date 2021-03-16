using System;
using System.Threading.Tasks;
using Library.Components.Services;

namespace Library.Components.Tests.Mocks
{
    public class MockMemberRegistry : IMemberRegistry
    {
        private readonly bool _validity;

        public MockMemberRegistry(bool validity = true)
        {
            _validity = validity;
        }
        
        public Task<bool> IsMemberValid(Guid memberId)
        {
            return Task.FromResult(_validity);
        }
    }
}