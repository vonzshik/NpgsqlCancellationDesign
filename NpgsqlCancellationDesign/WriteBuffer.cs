using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class WriteBuffer
    {
        private readonly Connector connector;

        public WriteBuffer(Connector connector)
        {
            this.connector = connector;
        }

        public async Task Write(bool async, int value)
        {
            try
            {
                if (async)
                    await this.connector.RWO.WriteAsync(value, CancellationToken.None);
                else
                    this.connector.RWO.Write(value);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
