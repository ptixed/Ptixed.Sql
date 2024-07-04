using Ptixed.Sql.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
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

        public override string ToString() => ToSql(new List<object>(), new MappingConfig());

        public DbCommand ToSql(DbCommand command, MappingConfig mapping)
        {
            var values = new List<object>();
            var text = ToSql(values, mapping);

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

        protected abstract string FormatTableName(string name);
        protected abstract string FormatColumnName(string name);

        private string ToSql(List<object> values, MappingConfig mapping)
        {
            var sb = new StringBuilder();
            foreach (var part in _parts)
                sb.Append(ToSqlPart(values, mapping, part));
            return sb.ToString();
        }

        private string ToSqlPart(List<object> values, MappingConfig mapping, FormattableString part)
        {
            var formants = new List<string>();
            foreach (var argument in part.GetArguments())
                ToSqlArgument(values, mapping, formants, argument);
            return string.Format(part.Format, formants.ToArray());
        }

        private void ToSqlArgument(List<object> values, MappingConfig mapping, List<string> formants, object argument)
        {
            switch (argument)
            {
                case null:
                    formants.Add("NULL");
                    break;
                case FormattableString fs:
                    formants.Add(ToSqlPart(values, mapping, fs));
                    break;
                case Query<TParameter> q:
                    formants.Add(q.ToSql(values, mapping));
                    break;
                case int i:
                    formants.Add(i.ToString());
                    break;
                case string s:
                    formants.Add($"@{values.Count}");
                    values.Add(s);
                    break;
                case Type type:
                    formants.Add(FormatTableName(Table.Get(type).Name));
                    break;
                case Table table:
                    formants.Add(FormatTableName(table.Name));
                    break;
                case PhysicalColumn pc:
                    formants.Add(FormatColumnName(pc.Name));
                    break;
                case ColumnValue cv:
                    formants.Add(FormatColumnName(cv.Name));
                    break;
                case IEnumerable ie:
                    var formants1 = new List<string>();
                    var enumerator = ie.GetEnumerator();
                    if (!enumerator.MoveNext())
                        formants1.Add("SELECT TOP 0 0");
                    else
                    {
                        ToSqlArgument(values, mapping, formants1, enumerator.Current);
                        while (enumerator.MoveNext())
                            ToSqlArgument(values, mapping, formants1, enumerator.Current);
                    }
                    formants.Add($"({string.Join(", ", formants1)})");
                    break;
                default:
                    formants.Add($"@{values.Count}");
                    values.Add(argument);
                    break;
            }
        }
    }
}
