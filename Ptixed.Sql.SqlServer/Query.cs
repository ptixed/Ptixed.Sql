using Ptixed.Sql.Meta;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace Ptixed.Sql.SqlServer
{
    public class Query : Query<SqlParameter>
    {
        public static Query Unsafe(string query) => new Query(FormattableStringFactory.Create(query));

        public Query() { }
        public Query(FormattableString query) : base(query) { }

        protected override bool Format(object o, ref int index, MappingConfig mapping, List<object> formants, List<object> values)
        {
            switch (o)
            {
                case Type t:
                    formants.Add(Table.Get(t).Name);
                    return true;
                case Table tm:
                    formants.Add(tm.Name);
                    return true;
                case PhysicalColumn pc:
                    formants.Add($"[{pc.Name}]");
                    return true;
                case ColumnValue cv:
                    formants.Add($"[{cv.Name}]");
                    return true;
            }
            return false;
        }
    }
}
