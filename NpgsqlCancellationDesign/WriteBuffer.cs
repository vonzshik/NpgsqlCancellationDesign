using System;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class WriteBuffer
    {
        private readonly Connector connector;

        private int currentTimeout;

        private CancellationTokenSource cts = new CancellationTokenSource();

        public int Timeout
        {
            get => this.currentTimeout;
            set
            {
                if (this.currentTimeout != value)
                {
                    this.currentTimeout = value;
                    this.connector.RWO.WriteTimeout = value;
                }
            }
        }

        internal int CtsAllocated { get; private set; }

        public WriteBuffer(Connector connector)
        {
            this.connector = connector;
        }

        public async Task Write(bool async, int value)
        {
            var token = CancellationToken.None;
            if (async && this.Timeout > 0)
            {
                token = this.cts.Token;
                this.cts.CancelAfter(this.Timeout);
            }

            try
            {
                if (async)
                    await this.connector.RWO.WriteAsync(value, token);
                else
                    this.connector.RWO.Write(value);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.cts.CancelAfter(-1);
                if (this.cts.IsCancellationRequested)
                {
                    this.cts.Dispose();
                    this.cts = new CancellationTokenSource();
                    this.CtsAllocated++;
                }
            }
        }
    }
}
