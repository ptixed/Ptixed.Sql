using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Ptixed.Sql.Attributes;
using Ptixed.Sql.Collections;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Tests.Specimens
{
    internal class JsonSqlConverter : ISqlConverter
    {
        private readonly string _name;
        public JsonSqlConverter(string name) => _name = name;

        public List<ColumnAttribute> GetColumns(PropertyInfo member)
            => new List<ColumnAttribute> { new ColumnAttribute(_name) };

        public List<ColumnValue> ToQuery(object value, LogicalColumn meta)
            => new List<ColumnValue> { new ColumnValue(_name, JsonConvert.SerializeObject(value)) };

        public object FromQuery(ColumnValueSet columns, LogicalColumn meta, MappingConfig mapping)
            => JsonConvert.DeserializeObject(columns[_name]?.ToString(), meta.Member.PropertyType);
    }
}
