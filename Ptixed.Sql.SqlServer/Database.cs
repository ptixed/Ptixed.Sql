using Ptixed.Sql.Impl;
using System.Data.Common;
using System.Data.SqlClient;

namespace Ptixed.Sql.SqlServer
{
    public class Database : Database<SqlCommand, SqlParameter>, IDatabase
    {
        public Database(ConnectionConfig config) : base(config)
        {
        }

        protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);
    }
}
