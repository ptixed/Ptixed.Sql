using System;

namespace Ptixed.Sql.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public bool IsAutoIncrement { get; set; }

        public readonly string ColumnName;

        public ColumnAttribute(string name = null)
        {
            ColumnName = name;
        }
    }
}
