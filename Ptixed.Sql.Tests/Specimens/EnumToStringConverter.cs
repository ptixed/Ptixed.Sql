using System;
using System.Collections.Generic;
using System.Reflection;
using Ptixed.Sql.Attributes;
using Ptixed.Sql.Collections;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Tests.Specimens
{
    internal class EnumToStringConverter : ISqlConverter
    {
        public List<ColumnAttribute> GetColumns(PropertyInfo member) 
            => null;

        public List<ColumnValue> ToQuery(object value, LogicalColumn meta)
            => new List<ColumnValue> { new ColumnValue(meta.PhysicalColumns[0].Name, value.ToString()) }; 

        public object FromQuery(ColumnValueSet columns, LogicalColumn meta, MappingConfig mapping)
            => Enum.Parse(meta.Member.PropertyType, columns[meta.PhysicalColumns[0].Name].ToString());
    }
}
