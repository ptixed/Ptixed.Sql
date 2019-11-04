using System;

namespace Ptixed.Sql.Impl
{
    public class ConnectionConfig
    {
        public readonly string ConnectionString;
        public readonly MappingConfig Mappping;
        public readonly TimeSpan CommandTimeout;

        public ConnectionConfig(string connection, MappingConfig mapping = null, TimeSpan? timeout = null)
        {
            ConnectionString = connection;
            Mappping = mapping ?? new MappingConfig();
            CommandTimeout = timeout ?? TimeSpan.FromMinutes(1);
        }
    }
}
