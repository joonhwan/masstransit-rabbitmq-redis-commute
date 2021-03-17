using System;
using System.Threading.Tasks;

namespace Library.Components.Services
{
    public interface IBookCollectionRepository
    {
        Task Add(Guid memberId, Guid bookId);
    }
}