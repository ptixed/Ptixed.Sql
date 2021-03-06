﻿using Ptixed.Sql.Attributes;
using Ptixed.Sql.Collections;
using Ptixed.Sql.Metadata;
using System.Collections.Generic;
using System.Reflection;

namespace Ptixed.Sql
{
    public interface ISqlConverter
    {
        List<ColumnAttribute> GetColumns(PropertyInfo member);

        List<ColumnValue> ToQuery(object value, LogicalColumn meta);
        object FromQuery(ColumnValueSet columns, LogicalColumn meta, MappingConfig mapping);
    }
}
