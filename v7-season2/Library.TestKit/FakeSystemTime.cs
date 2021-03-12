using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using Quartz;

namespace Library.TestKit
{
    public class FakeSystemTime : IDisposable
    {
        private readonly SchedulerProvider _schedulerProvider;
        
        private TimeSpan _testOffset = TimeSpan.Zero;
        private bool _disposed = false;

        private FakeSystemTime(SchedulerProvider schedulerProvider)
        {
            _schedulerProvider = schedulerProvider;

            DateTimeOffset GetUtcNow()
            {
                return DateTimeOffset.UtcNow + _testOffset;
            }

            DateTimeOffset GetNow()
            {
                return DateTimeOffset.Now + _testOffset;
            }
            SystemTime.UtcNow = GetUtcNow;
            SystemTime.Now = GetNow;
        }
        
        public async Task Advance(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration));

            var scheduler = await _schedulerProvider.GetSchedulerAsync();

            // 시간을 멈춘다.
            await scheduler.Standby().ConfigureAwait(false);

            // 다음 시간을 조정한뒤
            _testOffset += duration;

            // 시간이 흐르기 시작.
            await scheduler.Start().ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            SystemTime.UtcNow = () => DateTimeOffset.UtcNow;
            SystemTime.Now = () => DateTimeOffset.Now;
            _disposed = true;
        }

        public static FakeSystemTime For(InMemoryTestHarness testHarness)
        {
            var schedulerOwner = new SchedulerProvider();
            testHarness.OnConfigureInMemoryBus += configurator =>
            {
                configurator.UseInMemoryScheduler(out schedulerOwner.Scheduler);
            };
            return new FakeSystemTime(schedulerOwner);
        }
        
        class SchedulerProvider
        {
            internal Task<IScheduler> Scheduler;

            public async Task<IScheduler> GetSchedulerAsync()
            {
                return await Scheduler.ConfigureAwait(false);
            }
        }
    }
}