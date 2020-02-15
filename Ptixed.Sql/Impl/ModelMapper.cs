using Ptixed.Sql.Meta;
using Ptixed.Sql.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql.Impl
{
    public static class ModelMapper
    {
        public static object[] Map(MappingConfig config, Type[] types, ColumnValueSet columns)
        {
            int offset = 0;
            var ret = new object[types.Length];
            for (int i = 0; i < types.Length; ++i)
            {
                if (config.ScalarTypes.Contains(types[i]))
                {
                    ret[i] = MapScalar(config, types[i], columns.GetRange(offset, 1));
                    offset += 1;
                }
                else
                {
                    var table = Table.Get(types[i]);
                    ret[i] = MapModel(config, table, columns.GetRange(offset, table.PhysicalColumns.Length));
                    offset += table.PhysicalColumns.Length;
                }
            }
            return ret;
        }

        public static object Map(MappingConfig config, Type type, ColumnValueSet columns)
        {
            if (config.ScalarTypes.Contains(type))
                return MapScalar(config, type, columns);
            return MapModel(config, Table.Get(type), columns);
        }

        private static object MapModel(MappingConfig config, Table table, ColumnValueSet columns)
        {
            if (table.PrimaryKey != null)
                if (table.PrimaryKey.PhysicalColumns.All(x => columns[x.Name] == null))
                    return null;

            var model = table.CreateNew();
            foreach (var column in table.LogicalColumns)
                table[model, column] = column.FromQuery(columns, config);

            return model;
        }

        private static object MapScalar(MappingConfig config, Type type, ColumnValueSet columns)
        {
            return config.FromDb(type, columns.Single().Value);
        }

        public static List<object> ConsructObjectGraph(Type[] types, List<object[]> rows)
        {
            if (!rows.Any())
                return new List<object>();
            return ConstructNode(new Range<Type>(types), new Range2<object>(rows.ToArray(), rows.Count, rows[0].Length)).ToList();
        }

        private static IEnumerable<object> ConstructNode(Range<Type> types, Range2<object> rows)
        {
            var table = Table.Get(types[0]);

            var relations = new List<(int index, Relation relation)>();
            for (int i = 1; i < types.Length; ++i)
                if (table.Relations.TryGetValue(types[i], out Relation r))
                    relations.Add((i, r));
            relations.Add((types.Length, null));

            var roots = new List<(int index, object pk)>();
            object pk = null;
            for (int i = 0; i < rows.Length1; ++i)
            {
                var row = rows[i, 0];
                if (row != null)
                {
                    var newpk = table[row, table.PrimaryKey];
                    if (pk?.Equals(newpk) != true)
                    {
                        pk = newpk;
                        roots.Add((i, pk));
                    }
                }
            }
            roots.Add((rows.Length1, null));

            var done = new HashSet<object>();
            for (int roi = 0; roi < roots.Count - 1; ++roi)
            {
                var root = rows[roots[roi].index, 0];
                if (done.Contains(table[root, table.PrimaryKey]))
                    continue;

                for (var rei = 0; rei < relations.Count - 1; ++rei)
                {
                    var subts = types.GetRange(relations[rei].index, relations[rei + 1].index - relations[rei].index);
                    var subrows = rows.GetRange(
                        roots[roi].index,
                        roots[roi + 1].index - roots[roi].index,
                        relations[rei].index,
                        relations[rei + 1].index - relations[rei].index);

                    var nodes = ConstructNode(subts, subrows).ToList();
                    relations[rei].relation.SetValue(root, nodes);

                    if (Table.Get(subts[0]).Relations.TryGetValue(types[0], out Relation reverse))
                        foreach (var node in nodes)
                            reverse.SetValue(node, new List<object> { root });
                }

                yield return root;
                done.Add(table[root, table.PrimaryKey]);
            }
        }
    }
}
