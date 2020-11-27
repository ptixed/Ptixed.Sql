using System;

namespace Ptixed.Sql.Metadata
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public readonly string ColumnName;
        public bool IsAutoIncrement { get; set; }

        public ColumnAttribute(string name = null)
        {
            ColumnName = name;
        }
    }
}
