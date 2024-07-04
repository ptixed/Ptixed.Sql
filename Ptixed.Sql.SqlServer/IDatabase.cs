using System.Collections.Generic;
using System.Data.SqlClient;

namespace Ptixed.Sql.SqlServer
{
    public interface IDatabase : IDatabase<SqlParameter>
    {
        void BulkInsert<T>(IEnumerable<T> entries);
    }
}
