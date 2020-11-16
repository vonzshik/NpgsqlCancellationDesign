using System;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class ReadBuffer
    {
        private readonly Connector connector;

        private int currentTimeout;

        internal CancellationTokenSource Cts { get; private set; } = new CancellationTokenSource();

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
            var token = this.Cts.Token;
            if (async && this.Timeout > 0)
            {
                token = this.Cts.Token;
                this.Cts.CancelAfter(this.Timeout);
            }

            try
            {
                return async
                    ? await this.connector.RWO.ReadAsync(token)
                    : this.connector.RWO.Read();
            }
            catch (OperationCanceledException) when (this.connector.UserCancellationRequested)
            {
                throw;
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
                this.Cts.CancelAfter(-1);
                if (this.Cts.IsCancellationRequested)
                {
                    this.Cts.Dispose();
                    this.Cts = new CancellationTokenSource();
                    this.CtsAllocated++;
                }
            }
        }
    }
}
