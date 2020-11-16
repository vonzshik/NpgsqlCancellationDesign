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
        }

        [Test]
        [Timeout(1000)]
        public void WriteTimeoutSync()
        {
            var connector = new Connector();
            connector.WriteTimeout = 25;

            Assert.Throws<TimeoutException>(() => connector.Write(0));
        }

        [Test]
        [Timeout(1000)]
        public void WriteTimeoutAsync()
        {
            var connector = new Connector();
            connector.WriteTimeout = 25;

            Assert.ThrowsAsync<TimeoutException>(() => connector.WriteAsync(0));
        }

        [Test]
        [Timeout(1000)]
        public void ReadTimeoutSync()
        {
            var connector = new Connector();
            connector.ReadTimeout = 115;

            Assert.Throws<TimeoutException>(() => connector.Read());
        }

        [Test]
        [Timeout(1000)]
        public void ReadTimeoutAsync()
        {
            var connector = new Connector();
            connector.ReadTimeout = 115;

            Assert.ThrowsAsync<TimeoutException>(() => connector.ReadAsync());
        }
    }
}