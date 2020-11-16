using System;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class ReadBuffer
    {
        private readonly Connector connector;

        private int currentTimeout;

        private CancellationTokenSource cts = new CancellationTokenSource();

        internal int CtsAllocated { get; private set; }

        public int Timeout
        {
            get => this.currentTimeout;
            set
            {
                if (this.currentTimeout != value)
                {
                    this.currentTimeout = value;
                    this.connector.RWO.ReadTimeout = value;
                }
            }
        }

        public ReadBuffer(Connector connector)
        {
            this.connector = connector;
        }

        public async Task<int> Read(bool async)
        {
            var token = CancellationToken.None;
            if (async && this.Timeout > 0)
            {
                token = this.cts.Token;
                this.cts.CancelAfter(this.Timeout);
            }

            try
            {
                return async
                    ? await this.connector.RWO.ReadAsync(token)
                    : this.connector.RWO.Read();
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
