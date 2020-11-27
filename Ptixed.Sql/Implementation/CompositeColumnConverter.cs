using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ptixed.Sql.Attributes;
using Ptixed.Sql.Collections;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Implementation
{
    public class CompositeColumnConverter : ISqlConverter
    {
        public List<ColumnAttribute> GetColumns(PropertyInfo member)
        {
            var equals = member.PropertyType.GetMethod(nameof(Equals), new[] { typeof(object) });
            if (equals == null || equals.DeclaringType == typeof(object))
                throw PtixedException.MissingImplementation(member.PropertyType, nameof(Equals));

            return Table.Get(member.PropertyType)
                .PhysicalColumns
                .Select(x => new ColumnAttribute(x.Name)
                {
                    IsAutoIncrement = x.IsAutoIncrement
                })
                .ToList();
        }

        public List<ColumnValue> ToQuery(object value, LogicalColumn meta)
        {
            return Table.Get(meta.Member.PropertyType)
                .LogicalColumns
                .SelectMany(x => x.FromEntityToQuery(value))
                .ToList();
        }

        public object FromQuery(ColumnValueSet columns, LogicalColumn meta, MappingConfig mapping)
        {
            var accessor = Table.Get(meta.Member.PropertyType);
            var ret = accessor.CreateNew();
            foreach (var column in accessor.LogicalColumns)
                accessor[ret, column] = column.FromQuery(columns, mapping);
            return ret;
        }
    }
}
