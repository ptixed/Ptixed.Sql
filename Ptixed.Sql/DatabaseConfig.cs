using System;
using System.Data;

namespace Ptixed.Sql
{
    public class DatabaseConfig
    {
        public string ConnectionString;
        public MappingConfig Mappping;
        public TimeSpan CommandTimeout = TimeSpan.FromMinutes(1);
        public IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;

        public DatabaseConfig(string connection)
        {
            ConnectionString = connection;
        }
    }
}
