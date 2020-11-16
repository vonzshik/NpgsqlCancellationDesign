using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NpgsqlCancellationDesign
{
    public class ReadBuffer
    {
        private readonly Connector connector;

        public ReadBuffer(Connector connector)
        {
            this.connector = connector;
        }

        public async Task<int> Read(bool async)
        {
            try
            {
                return async
                    ? await this.connector.RWO.ReadAsync(CancellationToken.None)
                    : this.connector.RWO.Read();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
