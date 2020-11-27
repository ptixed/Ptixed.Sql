using Ptixed.Sql.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ptixed.Sql.Metadata
{
    public class LogicalColumn
    {
        public readonly Table Table;
        public readonly PhysicalColumn[] PhysicalColumns;
        public readonly PropertyInfo Member;
        public readonly ISqlConverter Converter;
        public readonly string Name;

        private LogicalColumn(Table table, PropertyInfo member, ColumnAttribute column, ISqlConverter converter)
        {
            Table = table;
            Member = member;
            Converter = converter;
            Name = member.Name.Split('.').Last();

            if (Converter != null)
            {
                var columns = Converter.GetColumns(Member);
                if (columns != null)
                    PhysicalColumns = columns.Select(x => new PhysicalColumn(this, x.ColumnName, x.IsAutoIncrement)).ToArray();
            }

            if (PhysicalColumns == null)
                PhysicalColumns = new[] { new PhysicalColumn(this, column?.ColumnName ?? Name, column?.IsAutoIncrement ?? false) };
        }

        public static LogicalColumn TryCreate(Table table, PropertyInfo member)
        {
            var attrs = member.GetCustomAttributes().ToList();

            var converter = attrs.OfType<SqlConverterAttribute>().FirstOrDefault();
            var column = attrs.OfType<ColumnAttribute>().FirstOrDefault();
            var ignore = attrs.OfType<IgnoreAttribute>().FirstOrDefault();
            var relation = attrs.OfType<RelationAttribute>().FirstOrDefault();

            if (ignore != null || relation != null)
                return null;

            if (column == null)
            {
                var ispublic = member.GetMethod?.IsPublic == true || member.SetMethod?.IsPublic == true;
                if (!ispublic)
                    return null;
            }

            return new LogicalColumn(table, member, column, converter?.CreateConverter());
        }

        public ICollection<ColumnValue> FromEntityToQuery(object value) => FromValueToQuery(Table[value, this]);
        public ICollection<ColumnValue> FromValueToQuery(object value)
        {
            if (Converter != null)
                return Converter.ToQuery(value, this);
            return new[] { new ColumnValue(PhysicalColumns[0].Name, value) };
        }

        public object FromQuery(ColumnValueSet columns, MappingConfig mapping)
        {
            if (Converter != null)
                return Converter.FromQuery(columns, this, mapping);
            return mapping.FromDb(Member.PropertyType, columns[PhysicalColumns[0].Name]);
        }

        public static bool operator ==(LogicalColumn l, LogicalColumn r) => l?.Equals(r) ?? ReferenceEquals(r, null);
        public static bool operator !=(LogicalColumn l, LogicalColumn r) => !(l == r);

        public override bool Equals(object obj) => obj is LogicalColumn cm && cm.Member.Equals(Member);
        public override int GetHashCode() => Member.GetHashCode();
    }
}
