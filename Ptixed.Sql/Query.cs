using Ptixed.Sql.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Ptixed.Sql
{
    public abstract class Query<TParameter> 
        where TParameter : DbParameter, new()
    {
        private readonly List<FormattableString> _parts =  new List<FormattableString>();
        public bool IsEmpty => _parts.Count == 0;

        public TimeSpan? Timeout;

        public Query() { }
        public Query(FormattableString query) => Append(query);

        public Query<TParameter> Append(FormattableString query)
        {
            _parts.Add(query);
            return this;
        }
        
        public Query<TParameter> Append(Query<TParameter> query)
        {
            _parts.AddRange(query._parts);
            return this;
        }

        public Query<TParameter> Append(FormattableString separator, IEnumerable<Query<TParameter>> parts)
        {
            using (var e = parts.GetEnumerator())
                if (e.MoveNext())
                {
                    Append(e.Current);
                    while (e.MoveNext())
                        Append(separator).Append(e.Current);
                }
            return this;
        }

        public override string ToString() => ToString(new MappingConfig());

        public string ToString(MappingConfig mc)
        {
            var index = 0;
            return ToSql(ref index, mc).Item1;
        }

        public DbCommand ToSql(DbCommand command, MappingConfig mapping)
        {
            var index = 0;
            var (text, values) = ToSql(ref index, mapping);

            command.CommandText = text;
            foreach (var (value, i) in values.Select((x, i) => (x, i)))
            {
                var dbvalue = mapping.ToDb(value?.GetType(), value);
                var parameter = dbvalue as DbParameter ?? new TParameter { Value = dbvalue };
                parameter.ParameterName = i.ToString();
                command.Parameters.Add(parameter);
            }

            if (Timeout != null)
                command.CommandTimeout = (int)Timeout.Value.TotalSeconds;

            return command;
        }

        protected abstract bool Format(object o, ref int index, MappingConfig mapping, List<object> formants, List<object> values);

        private (string, List<object>) ToSql(ref int index, MappingConfig mapping)
        {
            var sb = new StringBuilder();
            var values = new List<object>();
            foreach (var part in _parts)
            {
                var formants = new List<object>();
                foreach (var argument in part.GetArguments())
                {
                    if (Format(argument, ref index, mapping, formants, values))
                        continue;
                    switch (argument)
                    {
                        case null:
                            formants.Add("NULL");
                            break;
                        case Query<TParameter> q:
                            var (text, vs) = q.ToSql(ref index, mapping);
                            values.AddRange(vs);
                            formants.Add(text);
                            break;
                        case Type t:
                            formants.Add(mapping.FormatTableName(Table.Get(t)));
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
                }
                sb.Append(string.Format(part.Format, formants.ToArray()));                
            }
            return (sb.ToString(), values);
        }
    }
}
