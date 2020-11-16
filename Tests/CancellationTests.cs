using NpgsqlCancellationDesign;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public class CancellationTests
    {
        [Test]
        [Timeout(1000)]
        public void CancelAsync()
        {
            var connector = new Connector();
            using var cts = new CancellationTokenSource();
            var readTask = connector.ReadAsync(cts.Token);
            cts.Cancel();

            // Task.Delay throws TaskCanceledException instead of OperationCanceledException
            Assert.ThrowsAsync<TaskCanceledException>(async () => await readTask);
            Assert.That(connector.ReadCtsAllocated, Is.EqualTo(1));
            Assert.That(connector.WriteCtsAllocated, Is.EqualTo(0));
        }
    }
}
