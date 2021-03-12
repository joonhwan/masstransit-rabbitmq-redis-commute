using System;
using System.Threading.Tasks;
using Library.Contracts;
using Library.TestKit;
using MassTransit;
using MassTransit.Testing;
using NUnit.Framework;

namespace Library.Components.Tests
{
    public class Book신규추가 : StateMachineTestFixture<BookStateMachine, BookSaga>
    {
        [Test]
        public async Task 새로운_BookId_메시지를_받으면_새로운_Saga_Instance가_만들어진다()
        {
            var bookId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307959123",
                Title = "Gone with the Wind"
            });

            // 실제로 Message를 Serialize 하고, 전송하는 모든 과정이 Simulation 된다. 
            
            Assert.IsTrue(await TestHarness.Consumed.Any<BookAdded>(), "메시지 수신이 안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookAdded>(), "Saga에 의해 메시지 처리가 안됨");
            Assert.That(await SagaHarness.Created.Any(x => x.CorrelationId == bookId),
                "생성된 Saga의 CorrelationId 는 Book Id 여야 함");

            // var instance = SagaHarness.Created.ContainsInState(bookId, StateMachine, StateMachine.Available);
            // var wrongBookId = NewId.NextGuid();
            Assert.That(await SagaHarness.Exists(bookId, machine => machine.Available), Is.EqualTo(bookId), 
                "수신된 메시지의 BookId 에 해당하는 Saga가 없음.");
        }
    }
}