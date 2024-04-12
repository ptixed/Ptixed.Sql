using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace Ptixed.Sql.SqlServer
{
    public class Query : Query<SqlParameter>
    {
        public static Query Unsafe(string query) => new Query(FormattableStringFactory.Create(query));

        public Query() { }
        public Query(FormattableString query) : base(query) { }
    }
}
