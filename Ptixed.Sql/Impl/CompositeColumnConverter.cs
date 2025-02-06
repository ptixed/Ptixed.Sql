using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ptixed.Sql.Meta;
using Ptixed.Sql.Util;

namespace Ptixed.Sql.Impl
{
    public class CompositeColumnConverter : ISqlConverter
    {
        public List<ColumnAttribute> GetColumns(MemberInfo member)
        {
            var equals = member.GetMemberType().GetMethod(nameof(Equals), new[] { typeof(object) });
            if (equals == null || equals.DeclaringType == typeof(object))
                throw PtixedException.MissingImplementation(member.GetMemberType(), nameof(Equals));

            return Table.Get(member.GetMemberType())
                .PhysicalColumns
                .Select(x => new ColumnAttribute(x.Name)
                {
                    IsAutoIncrement = x.IsAutoIncrement
                })
                .ToList();
        }

        public List<ColumnValue> ToQuery(object value, LogicalColumn meta)
        {
            return Table.Get(meta.Member.GetMemberType())
                .LogicalColumns
                .SelectMany(x => x.FromEntityToQuery(value))
                .ToList();
        }

        public object FromQuery(ColumnValueSet columns, LogicalColumn meta, MappingConfig mapping)
        {
            var accessor = Table.Get(meta.Member.GetMemberType());
            var ret = accessor.CreateNew();
            foreach (var column in accessor.LogicalColumns)
                accessor[ret, column] = column.FromQuery(columns, mapping);
            return ret;
        }
    }
}
