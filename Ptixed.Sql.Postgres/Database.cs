using Npgsql;
using Ptixed.Sql.Impl;
using System.Data.Common;

namespace Ptixed.Sql.Postgres
{
    public class Database : Database<NpgsqlCommand, NpgsqlParameter>, IDatabase
    {
        public Database(ConnectionConfig config) : base(config)
        {
        }

        protected override DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);
    }
}
