using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class Connector
    {
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
            using var _ = cancellationToken.Register(this.ReadBuffer.Cancel);
            return await this.ReadBuffer.Read(async: true);
        }

        public void Reset()
        {
            this.ReadBuffer.Reset();
        }
    }
}
