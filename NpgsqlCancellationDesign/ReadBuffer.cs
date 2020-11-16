using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class ReadBuffer
    {
        private readonly Connector connector;

        private int currentTimeout = -1;

        internal CancellationTokenSource Cts { get; private set; } = new CancellationTokenSource();

        internal int CtsAllocated { get; private set; }

        /// <summary>
        /// Determines, if something is resetting the cts.
        /// 1, if it's unlocked.
        /// 0, if it's locked.
        /// </summary>
        private int resetCtsLock = 1;

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
            if (async && this.Timeout >= 0)
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
            catch (OperationCanceledException) when (this.connector.IsCancellationRequested)
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
                var lockTaken = Interlocked.Decrement(ref this.resetCtsLock);
                Debug.Assert(lockTaken == 0 || lockTaken == -1);
                if (lockTaken == -1)
                {
                    // Worst case scenario - we're attempting to cancel right now
                    // Waiting for it to complete
                    lock (this) { }
                }

                this.Cts.CancelAfter(-1);
                if (this.Cts.IsCancellationRequested)
                {
                    this.Cts.Dispose();
                    this.Cts = new CancellationTokenSource();

                    this.CtsAllocated++;

                    if (this.EnableCtsDisposeLock)
                    {
                        this.CtsDisposeUserLock.Set();
                        this.CtsDisposeBufferLock.Wait();
                    }
                }

                // Unlocking the cts
                Interlocked.Increment(ref this.resetCtsLock);

                //TODO: If we cancel between reads with a positive cancellation timeout, we might get a cancelled cts (although there was not waiting)
            }
        }

        internal void Cancel()
        {
            // We have to put a lock here, so it's not cancelled while it's being disposed
            lock (this)
            {
                var lockTaken = Interlocked.Decrement(ref this.resetCtsLock);
                Debug.Assert(lockTaken == 0 || lockTaken == -1);
                if (lockTaken == 0)
                {
                    // Best case scenario - we were able to take a lock
                    this.Cts.Cancel();
                }

                Interlocked.Increment(ref this.resetCtsLock);
            }
        }

        #region For Tests

        internal ManualResetEventSlim CtsDisposeUserLock { get; } = new ManualResetEventSlim();
        internal ManualResetEventSlim CtsDisposeBufferLock { get; } = new ManualResetEventSlim();

        internal bool EnableCtsDisposeLock { get; set; }

        #endregion
    }
}
