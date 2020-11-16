using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class Connector
    {
        private volatile bool isCancellationRequested;
        internal bool IsCancellationRequested => this.isCancellationRequested;

        internal ReadBuffer ReadBuffer { get; }
        internal WriteBuffer WriteBuffer { get; }

        internal ReadWriteObject RWO { get; } = new ReadWriteObject();

        public int ReadTimeout
        {
            get => this.ReadBuffer.Timeout;
            set => this.ReadBuffer.Timeout = value;
        }
        public int WriteTimeout
        {
            get => this.WriteBuffer.Timeout;
            set => this.WriteBuffer.Timeout = value;
        }

        public int ReadCtsAllocated => this.ReadBuffer.CtsAllocated;
        public int WriteCtsAllocated => this.WriteBuffer.CtsAllocated;

        public Connector()
        {
            this.ReadBuffer = new ReadBuffer(this);
            this.WriteBuffer = new WriteBuffer(this);
        }

        public void Write(int value) => this.WriteBuffer.Write(async: false, value).GetAwaiter().GetResult();
        public Task WriteAsync(int value) => this.WriteBuffer.Write(async: true, value);

        public int Read() => this.ReadBuffer.Read(async: false).GetAwaiter().GetResult();
        public async Task<int> ReadAsync(CancellationToken cancellationToken = default)
        {
            using var _ = cancellationToken.Register(this.Cancel);
            return await this.ReadBuffer.Read(async: true);
        }

        public async Task<int[]> ReadMultipleAsync(int count, CancellationToken cancellationToken = default)
        {
            var result = new List<int>(count);
            using var _ = cancellationToken.Register(this.Cancel);
            for (var i = 0; i < count; i++)
            {
                var read = await this.ReadBuffer.Read(async: true);
                result.Add(read);

                if (this.EnableMultipleReadLock)
                {
                    this.MultipleReadUserLock.Set();
                    this.MultipleReadConnectorLock.Wait();
                }
            }

            return result.ToArray();
        }

        public void Reset()
        {
            this.isCancellationRequested = false;
        }

        public void Cancel()
        {
            lock (this)
            {
                if (this.isCancellationRequested)
                    return;

                this.isCancellationRequested = true;
                this.ReadBuffer.Timeout = 0;
                this.ReadBuffer.Cancel();
            }
        }

        #region For Tests

        internal ManualResetEventSlim MultipleReadConnectorLock { get; } = new ManualResetEventSlim();
        internal ManualResetEventSlim MultipleReadUserLock { get; } = new ManualResetEventSlim();

        internal bool EnableMultipleReadLock { get; set; }

        #endregion
    }
}
