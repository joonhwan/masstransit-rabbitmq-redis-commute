using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Library.Components.Services;
using Library.Components.StateMachines;
using Library.Components.Tests.Mocks;
using Library.Contracts;
using Library.Contracts.Messages;
using Library.TestKit;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Logging;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Quartz;

namespace Library.Components.Tests
{
    public class TestCheckOutSettings : CheckOutSettings
    {
        public TimeSpan DefaultCheckOutDuration { get; } = TimeSpan.FromDays(14);
        public TimeSpan CheckOutDurationLimit { get; } = TimeSpan.FromDays(30);
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CheckOutSaga는_Book이_CheckOut되면 : StateMachineTestFixture<CheckOutStateMachine, CheckOutSaga>
    {
        protected override void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<CheckOutSettings>(new TestCheckOutSettings());
            services.AddScoped<IMemberRegistry>(provider => new MockMemberRegistry(true));
        }

        [Test]
        public async Task 새로운_BookId_메시지를_받으면_새로운_Saga_Instance가_만들어지고_Activity가_수행된다()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                Timestamp = InVar.Timestamp,
                MemberId = memberId
            });

            // 실제로 Message를 Serialize 하고, 전송하는 모든 과정이 Simulation 된다. 
            
            Assert.IsTrue(await TestHarness.Consumed.Any<BookCheckedOut>(), "메시지 수신이 안됨");
            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(), "메시지 수신이 안됨");
            
            var checkOutSaga = SagaHarness.SagaOf(checkOutId);
            Assert.IsTrue(await checkOutSaga.Created(), "Saga 생성 안됨");
            Assert.IsTrue(await checkOutSaga.ExistsAs(m => m.CheckedOut), "CheckOut 상태가 아님.");

            Assert.IsTrue(await TestHarness.Published.Any<NotifyMemberDueDate>(), "NotifyMemberDueDate 메시지 publish 안됨");
        }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CheckOutSaga는_CheckOut이_Renew되면 : StateMachineTestFixture<CheckOutStateMachine, CheckOutSaga>
    {
        protected override void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<CheckOutSettings>(new TestCheckOutSettings());
            services.AddScoped<IMemberRegistry>(provider => new MockMemberRegistry(true));
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator cfg)
        {
            cfg.AddRequestClient<RenewCheckOut>();
        }

        [Test]
        public async Task 현재시각을_시각을_기준으로_만료기간이_갱신된다()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            DateTime checkOutTime = InVar.Timestamp; 

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                Timestamp = checkOutTime,
                MemberId = memberId
            });
            
            var checkOutSaga = SagaHarness.SagaOf(checkOutId);
            Assert.IsTrue(await checkOutSaga.ExistsAs(m => m.CheckedOut), "CheckOut 상태가 아님.");

            // RenewCheckOut 전송.
            var settings = Provider.GetRequiredService<CheckOutSettings>();
            var requestClient = Provider.GetRequiredService<IRequestClient<RenewCheckOut>>();

            var now = Time.UtcNow;
            var renewed = await requestClient.GetResponse<CheckOutRenewed>(new
            {
                CheckOutId = checkOutId
            });
            Assert.That(renewed.Message.DueDate, Is.GreaterThanOrEqualTo(now + settings.DefaultCheckOutDuration));
            
            Assert.That(await TestHarness.Published.SelectAsync<NotifyMemberDueDate>().Count(), Is.EqualTo(2),
                "NotifyMemberDueDate 메시지 2번 publish 안됨");
        }
        
        [Test]
        public async Task 현재시각을_시각을_기준으로_만료기간이_갱신되지만_최대_허용_납기는_넘기지_않는다()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            DateTime checkOutTime = InVar.Timestamp; 

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                Timestamp = checkOutTime,
                MemberId = memberId
            });
            
            var checkOutSaga = SagaHarness.SagaOf(checkOutId);
            Assert.IsTrue(await checkOutSaga.ExistsAs(m => m.CheckedOut), "CheckOut 상태가 아님.");

            // RenewCheckOut 전송.
            var settings = Provider.GetRequiredService<CheckOutSettings>();
            var requestClient = Provider.GetRequiredService<IRequestClient<RenewCheckOut>>();

            var prev = Time.UtcNow;
            await Time.Advance(TimeSpan.FromDays(16));

            var now = Time.UtcNow;
            Assert.That(now - prev, Is.GreaterThanOrEqualTo(TimeSpan.FromDays(16)));
            
            // 원래는 아래처럼 시도했는데, Chris는 다른 방법을 사용.
            // var (renewed, limitReached) = await requestClient.GetResponse<CheckOutRenewed, CheckOutDurationLimitReached>(new
            // {
            //     CheckOutId = checkOutId
            // });
            // Assert.IsTrue(limitReached.IsCompletedSuccessfully);
            // Assert.That((await limitReached).Message.DueDate, Is.LessThanOrEqualTo(now + settings.DefaultCheckOutDuration));

            using (var request = requestClient.Create(new
            {
                CheckOutId = checkOutId
            }))
            {
                var renewed = request.GetResponse<CheckOutRenewed>(false); // false = "아직 보내지 마. 그냥 CheckOutRenewed 형 응답이 올지도 모른다는 것만 알아둬"
                var notFound = request.GetResponse<CheckOutNotFound>(false);
                var limitReached = request.GetResponse<CheckOutDurationLimitReached>(); //  true = "응. 이제 Send 해"

                await Task.WhenAny(renewed, notFound, limitReached);

                Assert.That(renewed.IsCompletedSuccessfully, Is.False);
                Assert.That(notFound.IsCompletedSuccessfully, Is.False);
                Assert.That(limitReached.IsCompletedSuccessfully, Is.True);

                Assert.That((await limitReached).Message.DueDate,
                    Is.LessThanOrEqualTo(now + settings.DefaultCheckOutDuration));
            }
        }

        
        [Test]
        public async Task CheckOut되지_않은_경우는_실패한다()
        {
            var checkOutId = NewId.NextGuid();
            
            var requestClient = Provider.GetRequiredService<IRequestClient<RenewCheckOut>>();
            var (renewed, notFound) = await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound>(new
            {
                CheckOutId = checkOutId
            });
            Assert.IsFalse(renewed.IsCompletedSuccessfully);
            Assert.IsTrue(notFound.IsCompletedSuccessfully);
            Assert.That((await notFound).Message.CheckOutId, Is.EqualTo(checkOutId));
        }

    }

}