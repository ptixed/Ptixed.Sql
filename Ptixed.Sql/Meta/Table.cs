using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ptixed.Sql.Impl;

namespace Ptixed.Sql.Meta
{
    public class Table
    {
        private static readonly ConcurrentDictionary<Type, Table> Cache = new ConcurrentDictionary<Type, Table>();

        private readonly Accessor<LogicalColumn> _accessor;

        public readonly string Name;

        public readonly LogicalColumn[] LogicalColumns;
        public readonly LogicalColumn PrimaryKey;

        public readonly PhysicalColumn.WithPk[] PhysicalColumns;
        public readonly PhysicalColumn.WithPk AutoIncrementColumn;

        public readonly Dictionary<Type, Relation> Relations;

        public object this[object target, LogicalColumn column]
        {
            get => _accessor[target, column];
            set => _accessor[target, column] = value;
        }

        public static Table Get(Type type) => Cache.GetOrAdd(type, x => new Table(x));

        private Table(Type type)
        {
            var lookup = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(x => LogicalColumn.TryCreate(this, x))
                .Where(x => x != null)
                .ToDictionary<LogicalColumn, LogicalColumn, MemberInfo>(x => x, x => x.Member);

            _accessor = new Accessor<LogicalColumn>(type, lookup);

            LogicalColumns = _accessor.Lookup.Keys.ToArray();

            var attr = type.GetCustomAttribute<TableAttribute>();
            Name = attr?.TableName ?? type.Name;
            if (attr?.PkColumn != null)
            {
                PrimaryKey = LogicalColumns.FirstOrDefault(x => x.Member.Name == attr.PkColumn);
                if (PrimaryKey == null)
                    throw PtixedException.InvalidColumnName(attr.PkColumn);
            }

            PhysicalColumns = LogicalColumns.SelectMany(x => x.PhysicalColumns.Select(y => y.AddPk(x == PrimaryKey))).ToArray();
            AutoIncrementColumn = PhysicalColumns.SingleOrDefault(x => x.IsAutoIncrement);
            
            Relations = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(x => Relation.TryCreate(x))
                .Where(x => x != null)
                .ToDictionary(x => x.SlotType);
        }

        public static bool operator ==(Table l, Table r) => l?.Equals(r) ?? ReferenceEquals(r, null);
        public static bool operator !=(Table l, Table r) => !(l == r);

        public override string ToString() => $"[{Name}]";
        public override bool Equals(object obj) => obj is Table t && t.Name == Name;
        public override int GetHashCode() => Name.GetHashCode();

        public object CreateNew() => _accessor.CreateNew();

        public Query GetPrimaryKeyCondition(object o)
        {
            var query = new Query();
            query.Append("(");
            query.Append(Query.Join(" AND ", PrimaryKey.FromValueToQuery(o).Select(column => new Query(() => $"{column} = {column.Value}"))));
            query.Append(")");
            return query;
        }

        public ICollection<ColumnValue> ToQuery(List<PhysicalColumn> columns, object o)
        {
            return columns
                .Select(x => x.LogicalColumn)
                .Distinct()
                .SelectMany(column => column.FromEntityToQuery(o))
                .Where(x => columns.Any(y => x.Name == y.Name))
                .ToList();
        }
    }
}
