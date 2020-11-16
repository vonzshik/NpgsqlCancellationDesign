using NpgsqlCancellationDesign;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Tests
{
    public class BasicTests
    {
        [Test]
        [Timeout(1000)]
        public async Task WriteAndRead([Values(true, false)] bool async)
        {
            var connector = new Connector();

            var expected = 42;

            if (async)
                await connector.WriteAsync(expected);
            else
                connector.Write(expected);
            var result = async
                ? await connector.ReadAsync()
                : connector.Read();

            Assert.That(result, Is.EqualTo(expected));
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(0));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }

        [Test]
        [Timeout(1000)]
        public void WriteTimeoutSync()
        {
            var connector = new Connector();
            connector.WriteTimeout = 25;

            Assert.Throws<TimeoutException>(() => connector.Write(0));
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(0));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }

        [Test]
        [Timeout(1000)]
        public void WriteTimeoutAsync()
        {
            var connector = new Connector();
            connector.WriteTimeout = 25;

            Assert.ThrowsAsync<TimeoutException>(() => connector.WriteAsync(0));
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(0));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(1));
        }

        [Test]
        [Timeout(1000)]
        public void ReadTimeoutSync()
        {
            var connector = new Connector();
            connector.ReadTimeout = 115;

            Assert.Throws<TimeoutException>(() => connector.Read());
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(0));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }

        [Test]
        [Timeout(1000)]
        public void ReadTimeoutAsync()
        {
            var connector = new Connector();
            connector.ReadTimeout = 115;

            Assert.ThrowsAsync<TimeoutException>(() => connector.ReadAsync());
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(1));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }

        [Test]
        [Timeout(5000)]
        public async Task WriteAndReadMultiple([Values(true, false)] bool async)
        {
            var connector = new Connector();

            var writeTask = Task.Run(async () =>
            {
                for (var i = 0; i < 5; i++)
                {
                    if (async)
                        await connector.WriteAsync(i);
                    else
                        connector.Write(i);
                }
            });

            var readTask = Task.Run(async () =>
            {
                for (var i = 0; i < 5; i++)
                {
                    var result = async
                        ? await connector.ReadAsync()
                        : connector.Read();

                    Assert.That(result, Is.EqualTo(i));
                }
            });

            Assert.DoesNotThrowAsync(() => writeTask);
            Assert.DoesNotThrowAsync(() => readTask);

            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(0));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }
    }
}