using System;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class ReadWriteObject
    {
        private static readonly Random rnd = new Random();

        private int? currentValue;

        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }

        public void Write(int value) => Task.Run(() => this.WriteInternal(async: false, value, CancellationToken.None)).GetAwaiter().GetResult();

        public Task WriteAsync(int value, CancellationToken cancellationToken) => Task.Run(() => this.WriteInternal(async: true, value, cancellationToken));

        private async Task WriteInternal(bool async, int value, CancellationToken cancellationToken)
        {
            var delay = rnd.Next(50, 100);
            var raiseTimeoutError = false;
            if (this.WriteTimeout > 0 && this.WriteTimeout < 50)
            {
                delay = this.WriteTimeout;
                raiseTimeoutError = true;
            }

            if (async)
                await Task.Delay(delay, cancellationToken);
            else
                Thread.Sleep(delay);

            if (raiseTimeoutError)
                throw new TimeoutException();

            this.currentValue = value;
        }

        public int Read() => this.ReadInternal(async: false, CancellationToken.None).GetAwaiter().GetResult();

        public Task<int> ReadAsync(CancellationToken cancellationToken) => this.ReadInternal(async: true, cancellationToken);

        private async Task<int> ReadInternal(bool async, CancellationToken cancellationToken)
        {
            var timePassed = 0;

            while (this.currentValue is null)
            {
                if (async)
                    await Task.Delay(33, cancellationToken);
                else
                    Thread.Sleep(33);
                timePassed += 33;

                if (this.ReadTimeout > 0 && timePassed > this.ReadTimeout)
                    throw new TimeoutException();
            }

            return this.currentValue.Value;
        }
    }
}
