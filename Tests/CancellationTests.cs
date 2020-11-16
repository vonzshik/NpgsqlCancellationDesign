using NpgsqlCancellationDesign;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public class CancellationTests
    {
        [Test]
        [Timeout(1000)]
        public async Task CancelAsync([Values(true, false)] bool isTokenCancelled)
        {
            var connector = new Connector();
            connector.ReadTimeout = 100;

            using var cts = new CancellationTokenSource();
            if (isTokenCancelled)
                cts.Cancel();
            var readTask = connector.ReadAsync(cts.Token);
            cts.Cancel();

            // Task.Delay throws TaskCanceledException instead of OperationCanceledException
            Assert.ThrowsAsync<TaskCanceledException>(async () => await readTask);

            connector.Reset();

            Assert.ThrowsAsync<TimeoutException>(() => connector.ReadAsync());

            await connector.WriteAsync(42);
            var result = await connector.ReadAsync();
            Assert.That(result, Is.EqualTo(42));

            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(2));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }

        [Test]
        [Timeout(1000)]
        public void CancelAsyncWhileDisposingCts()
        {
            var connector = new Connector();
            connector.ReadTimeout = 100;
            var readBuffer = connector.ReadBuffer;
            readBuffer.EnableCtsDisposeLock = true;

            using var cts = new CancellationTokenSource();
            var readTask = connector.ReadAsync(cts.Token);
            readBuffer.CtsDisposeUserLock.Wait();
            cts.Cancel();
            readBuffer.CtsDisposeBufferLock.Set();

            Assert.ThrowsAsync<TimeoutException>(async () => await readTask);
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(1));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
            Assert.That(!readBuffer.Cts.IsCancellationRequested);
        }

        [Test]
        [Timeout(5000)]
        public async Task CancelAsyncBetweenReadings()
        {
            var connector = new Connector();
            connector.ReadTimeout = 100;
            connector.EnableMultipleReadLock = true;

            await connector.WriteAsync(42);

            using var cts = new CancellationTokenSource();
            var readTask = Task.Run(() => connector.ReadMultipleAsync(2, cts.Token));
            connector.MultipleReadUserLock.Wait();
            cts.Cancel();
            connector.MultipleReadConnectorLock.Set();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await readTask);
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(1));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }
    }
}
