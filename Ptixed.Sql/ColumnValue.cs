﻿using System;

namespace Ptixed.Sql
{
    public class ColumnValue
    {
        public readonly string Name;
        public readonly object Value;

        public ColumnValue(string name, object value)
        {
            Name = name;
            Value = value == DBNull.Value ? null : value;
        }

        public void Deconstruct(out string name, out object value)
        {
            name = Name;
            value = Value;
        }
    }
}
