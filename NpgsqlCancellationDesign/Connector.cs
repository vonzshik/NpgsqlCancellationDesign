using System;
using System.Collections.Generic;
using System.Text;
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
            get => this.RWO.ReadTimeout;
            set => this.RWO.ReadTimeout = value;
        }

        public int WriteTimeout
        {
            get => this.RWO.WriteTimeout;
            set => this.RWO.WriteTimeout = value;
        }

        public Connector()
        {
            this.readBuffer = new ReadBuffer(this);
            this.writeBuffer = new WriteBuffer(this);
        }

        public void Write(int value) => this.writeBuffer.Write(async: false, value).GetAwaiter().GetResult();
        public Task WriteAsync(int value) => this.writeBuffer.Write(async: true, value);

        public int Read() => this.readBuffer.Read(async: false).GetAwaiter().GetResult();
        public Task<int> ReadAsync(CancellationToken cancellationToken = default) => this.readBuffer.Read(async: true);
    }
}
