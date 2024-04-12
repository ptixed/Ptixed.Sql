using Npgsql;
using Ptixed.Sql.Meta;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ptixed.Sql.Postgres
{
    public class Query : Query<NpgsqlParameter>
    {
        public static Query Unsafe(string query) => new Query(FormattableStringFactory.Create(query));

        public Query() { }
        public Query(FormattableString query) : base(query) { }

        protected override bool Format(object o, ref int index, MappingConfig mapping, List<object> formants, List<object> values)
        {
            switch (o)
            {
                case Table tm:
                    formants.Add($"\"{mapping.FormatTableName(tm)}\"");
                    return true;
                case PhysicalColumn pc:
                    formants.Add($"\"{pc.Name}\"");
                    return true;
                case ColumnValue cv:
                    formants.Add($"\"{cv.Name}\"");
                    return true;
            }
            return false;
        }
    }
}
