using Npgsql;
using Ptixed.Sql.Impl;

namespace Ptixed.Sql.Postgres
{
    public class Database : Database<NpgsqlConnection, NpgsqlCommand, NpgsqlParameter>, IDatabase
    {
        public Database(ConnectionConfig config) : base(config)
        {
        }
    }
}
