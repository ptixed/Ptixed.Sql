namespace Ptixed.Sql.Meta
{
    public class PhysicalColumn
    {
        public readonly LogicalColumn LogicalColumn;
        public readonly string Name;
        public readonly bool IsAutoIncrement;

        public PhysicalColumn(LogicalColumn column, string name, bool isai)
        {
            LogicalColumn = column;
            Name = name;
            IsAutoIncrement = isai;
        }

        public WithPk AddPk(bool ispk) => new WithPk(LogicalColumn, Name, IsAutoIncrement, ispk);

        public static bool operator ==(PhysicalColumn l, PhysicalColumn r) => l?.Equals(r) ?? ReferenceEquals(r, null);
        public static bool operator !=(PhysicalColumn l, PhysicalColumn r) => !(l == r);

        public override string ToString() => $"[{Name}]";
        public override bool Equals(object obj) => obj is PhysicalColumn wl && wl.LogicalColumn == LogicalColumn && wl.Name == Name;
        public override int GetHashCode() => LogicalColumn.GetHashCode() ^ Name.GetHashCode();

        public class WithPk : PhysicalColumn
        {
            public readonly bool IsPrimaryKey;

            public WithPk(LogicalColumn column, string name, bool isai, bool ispk)
                : base(column, name, isai)
            {
                IsPrimaryKey = ispk;
            }
        }
    }
}
