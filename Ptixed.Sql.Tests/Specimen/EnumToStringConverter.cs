using System;
using System.Collections.Generic;
using System.Reflection;
using Ptixed.Sql.Meta;
using Ptixed.Sql.Util;

namespace Ptixed.Sql.Tests.Specimen
{
    internal class EnumToStringConverter : ISqlConverter
    {
        public List<ColumnAttribute> GetColumns(MemberInfo member) 
            => null;

        public List<ColumnValue> ToQuery(object value, LogicalColumn meta)
            => new List<ColumnValue> { new ColumnValue(meta.PhysicalColumns[0].Name, value.ToString()) }; 

        public object FromQuery(ColumnValueSet columns, LogicalColumn meta, MappingConfig mapping)
            => Enum.Parse(meta.Member.GetMemberType(), columns[meta.PhysicalColumns[0].Name].ToString());
    }
}
