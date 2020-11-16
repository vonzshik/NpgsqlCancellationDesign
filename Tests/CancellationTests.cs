﻿using NpgsqlCancellationDesign;
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
    }
}
