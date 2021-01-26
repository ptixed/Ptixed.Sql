using System.Data.SqlClient;

namespace Ptixed.Sql
{
    public interface IDatabaseCore
    {
        DatabaseConfig Config { get; }
        SqlCommand CreateCommand();
    }
}
