using Npgsql;

namespace Ptixed.Sql.Postgres
{
    public interface IDatabase : IDatabase<NpgsqlParameter>
    {
    }
}
