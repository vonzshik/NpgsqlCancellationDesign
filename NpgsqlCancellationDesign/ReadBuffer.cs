using System;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class ReadBuffer
    {
        private readonly Connector connector;

        private int currentTimeout;

        private bool cancellationMode;

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
            catch (OperationCanceledException) when (this.cancellationMode)
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
                    // We have to put a lock here, so it's not cancelled while it's being disposed
                    lock (this)
                    {
                        this.Cts.Dispose();
                        this.Cts = new CancellationTokenSource();
                    }
                    
                    this.CtsAllocated++;
                }
            }
        }

        internal void Cancel()
        {
            // We have to put a lock here, so it's not cancelled while it's being disposed
            lock (this)
            {
                if (this.cancellationMode)
                    return;

                this.cancellationMode = true;
                this.Cts.Cancel();
            }
        }

        internal void Reset()
        {
            this.cancellationMode = false;
        }
    }
}
