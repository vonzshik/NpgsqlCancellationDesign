using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class Connector
    {
        private readonly ReadBuffer readBuffer;
        private readonly WriteBuffer writeBuffer;

        internal ReadWriteObject RWO { get; } = new ReadWriteObject();

        public int ReadTimeout
        {
            get => this.readBuffer.Timeout;
            set => this.readBuffer.Timeout = value;
        }
        public int WriteTimeout
        {
            get => this.writeBuffer.Timeout;
            set => this.writeBuffer.Timeout = value;
        }

        public int ReadCtsAllocated => this.readBuffer.CtsAllocated;
        public int WriteCtsAllocated => this.writeBuffer.CtsAllocated;

        public Connector()
        {
            this.readBuffer = new ReadBuffer(this);
            this.writeBuffer = new WriteBuffer(this);
        }

        public void Write(int value) => this.writeBuffer.Write(async: false, value).GetAwaiter().GetResult();
        public Task WriteAsync(int value) => this.writeBuffer.Write(async: true, value);

        public int Read() => this.readBuffer.Read(async: false).GetAwaiter().GetResult();
        public async Task<int> ReadAsync(CancellationToken cancellationToken = default)
        {
            using var _ = cancellationToken.Register(this.readBuffer.Cancel);
            return await this.readBuffer.Read(async: true);
        }

        public void Reset()
        {
            this.readBuffer.Reset();
        }
    }
}
