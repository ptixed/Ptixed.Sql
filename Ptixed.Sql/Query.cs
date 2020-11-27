using Ptixed.Sql.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ptixed.Sql
{
    public class Query
    {
        private readonly List<FormattableString> _parts =  new List<FormattableString>();

        public TimeSpan? Timeout;

        public Query() { }
        public Query(FormattableString query) => Append(query);

        public static Query Unsafe(string query) => new Query(FormattableStringFactory.Create(query));

        public Query Append(FormattableString query)
        {
            _parts.Add(query);
            return this;
        }
        
        public Query Append(Query query)
        {
            _parts.AddRange(query._parts);
            return this;
        }

        public Query Append(FormattableString separator, IEnumerable<Query> parts) => Append(Join(separator, parts));

        public static Query Join(FormattableString separator, IEnumerable<Query> parts)
        {
            var query = new Query();
            using (var e = parts.GetEnumerator())
                if (e.MoveNext())
                {
                    query.Append(e.Current);
                    while(e.MoveNext())
                        query.Append(separator).Append(e.Current);
                }
            return query;
        }
        
        public override string ToString() => ToSql(new SqlCommand(), new MappingConfig()).CommandText;

        public SqlCommand ToSql(SqlCommand command, MappingConfig mapping)
        {
            var index = 0;
            var (text, values) = ToSql(ref index, mapping);

            command.CommandText = text;
            foreach (var (value, i) in values.Select((x, i) => (x, i)))
            {
                var dbvalue = mapping.ToDb(value?.GetType(), value);
                var parameter = dbvalue as SqlParameter ?? new SqlParameter { Value = dbvalue };
                parameter.ParameterName = i.ToString();
                command.Parameters.Add(parameter);
            }

            if (Timeout != null)
                command.CommandTimeout = (int)Timeout.Value.TotalSeconds;

            return command;
        }

        private (string, List<object>) ToSql(ref int index, MappingConfig mapping)
        {
            var sb = new StringBuilder();
            var values = new List<object>();
            foreach (var part in _parts)
            {
                var formants = new List<object>();
                foreach (var argument in part.GetArguments())
                    switch (argument)
                    {
                        case null:
                            formants.Add("NULL");
                            break;
                        case Query q:
                            var (text, vs) = q.ToSql(ref index, mapping);
                            values.AddRange(vs);
                            formants.Add(text);
                            break;
                        case Table tm:
                            formants.Add(mapping.FormatTableName(tm));
                            break;
                        case Type t:
                            formants.Add(mapping.FormatTableName(Table.Get(t)));
                            break;
                        case PhysicalColumn pc:
                            formants.Add(pc.ToString());
                            break;
                        case ColumnValue cv:
                            formants.Add(cv.ToString());
                            break;
                        case int i:
                            formants.Add(i.ToString());
                            break;
                        case string s:
                            values.Add(s);
                            formants.Add("@" + index++.ToString());
                            break;
                        case IEnumerable ie:
                            var sb1 = new StringBuilder("(");
                            using (var enumerator = ie.Cast<object>().GetEnumerator())
                            {
                                if (!enumerator.MoveNext())
                                    sb1.Append("SELECT TOP 0 0");
                                else
                                {
                                    values.Add(enumerator.Current);
                                    sb1.Append("@" + index++.ToString());
                                    while (enumerator.MoveNext())
                                    {
                                        sb1.Append(", ");
                                        values.Add(enumerator.Current);
                                        sb1.Append("@" + index++.ToString());
                                    }
                                }
                            }
                            sb1.Append(")");
                            formants.Add(sb1.ToString());
                            break;
                        default:
                            values.Add(argument);
                            formants.Add("@" + index++.ToString());
                            break;
                    }
                sb.Append(string.Format(part.Format, formants.ToArray()));                
            }
            return (sb.ToString(), values);
        }
    }
}
