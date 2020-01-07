using Ptixed.Sql.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Ptixed.Sql
{
    public class Query
    {
        private readonly List<FormattableString> _parts =  new List<FormattableString>();
        public bool IsEmpty => _parts.Count == 0;

        public Query() { }
        public Query(FormattableString query) => Append(query);

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
            return ToSql(ref index, command, mapping);
        }

        private SqlCommand ToSql(ref int index, SqlCommand command, MappingConfig mapping)
        {
            void AddParameter(int i, object value)
            {
                var dbvalue = mapping.ToDb(value?.GetType(), value);
                var parameter = dbvalue as SqlParameter ?? new SqlParameter { Value = value };
                parameter.ParameterName = i.ToString();
                command.Parameters.Add(parameter);
            }

            var sb = new StringBuilder();
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
                            q.ToSql(ref index, command, mapping);
                            break;
                        case Table tm:
                            formants.Add(tm.ToString());
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
                            AddParameter(index, s);
                            formants.Add("@" + index++.ToString());
                            break;
                        case IEnumerable ie:
                            var sb1 = new StringBuilder("(");
                            using (var enumerator = ie.Cast<object>().GetEnumerator())
                            {
                                if (!enumerator.MoveNext())
                                    sb.Append("SELECT TOP 0 0");
                                else
                                {
                                    AddParameter(index, enumerator.Current);
                                    sb1.Append("@" + index++.ToString());
                                    while (enumerator.MoveNext())
                                    {
                                        sb1.Append(", ");
                                        AddParameter(index, enumerator.Current);
                                        sb1.Append("@" + index++.ToString());
                                    }
                                }
                            }
                            sb1.Append(")");
                            formants.Add(sb1.ToString());
                            break;
                        default:
                            AddParameter(index, argument);
                            formants.Add("@" + index++.ToString());
                            break;
                    }
                sb.Append(string.Format(part.Format, formants.ToArray()));                
            }
            command.CommandText += sb.ToString();
            return command;
        }
    }
}
