using System;

namespace Ptixed.Sql.Meta
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public readonly string TableName;
        public readonly string PkColumn;

        public TableAttribute(string name, string pk = null)
        {
            TableName = name;
            PkColumn = pk;
        }
    }
}
