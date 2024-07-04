using Npgsql;
using System;
using System.Runtime.CompilerServices;

namespace Ptixed.Sql.Postgres
{
    public class Query : Query<NpgsqlParameter>
    {
        public static Query Unsafe(string query) => new Query(FormattableStringFactory.Create(query));

        public Query() { }
        public Query(FormattableString query) : base(query) { }

        protected override string FormatTableName(string name) => $"\"{name}\"";
        protected override string FormatColumnName(string name) => $"\"{name}\"";
    }
}
