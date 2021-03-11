using System;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Sample.Platform.Commands;
using Sample.Platform.Consumers;

namespace Sample.Platform.Test
{
    [TestFixture]
    public class SampleConsumerSpecs
    {
        [Test]
        public async Task ShouldConsumeSampleCommand()
        {
            var harness = new InMemoryTestHarness();
            var consumer =
                harness.Consumer<SampleConsumer>(() => new SampleConsumer(NullLogger<SampleConsumer>.Instance));

            await harness.Start();

            try
            {
                await harness.InputQueueSendEndpoint.Send<SampleCommand>(new
                {
                    CommandId = InVar.Id,
                    Command = "Go and get beer for my wife"
                });
                Assert.That(consumer.Consumed.Select<SampleCommand>().Any(), Is.True);
            }
            finally
            {
                await harness.Stop();
            }
        }
        
    }
}