using System;

namespace Ptixed.Sql.Meta
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
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
