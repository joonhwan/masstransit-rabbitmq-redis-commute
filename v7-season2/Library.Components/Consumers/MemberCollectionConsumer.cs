using System;
using System.Threading.Tasks;
using Library.Components.Services;
using Library.Contracts.Messages;
using MassTransit;

namespace Library.Components.Consumers
{
    public class MemberCollectionConsumer : IConsumer<AddBookToMemberCollection>
    {
        private readonly IBookCollectionRepository _repository;

        public MemberCollectionConsumer(IBookCollectionRepository repository)
        {
            _repository = repository;
        }
        public async Task Consume(ConsumeContext<AddBookToMemberCollection> context)
        {
            await _repository.Add(context.Message.MemberId, context.Message.BookId);

            // @one-reason-to-use-message-initializer 
            // 
            // 세상에, 속성 구성이 완전히 동일한 서로 다른 2개의 type 들 간 대입을 통해
            // 아래처럼 쉽게 처리하네.
            await context.Publish<BookAddedToMemberCollection>(context.Message); 
        }
    }
}