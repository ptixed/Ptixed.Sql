using Ptixed.Sql.Metadata;
using Ptixed.Sql.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql.Implementation
{
    internal static class ModelMapper
    {
        public static object[] Map(MappingConfig config, ITracker tracker, Type[] types, ColumnValueSet columns)
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
                    ret[i] = MapModel(config, tracker, table, columns.GetRange(offset, table.PhysicalColumns.Length));
                    offset += table.PhysicalColumns.Length;
                }
            }
            if (offset != columns.Count)
                throw PtixedException.InvalidMapping();
            return ret;
        }

        public static object Map(MappingConfig config, ITracker tracker, Type type, ColumnValueSet columns)
        {
            if (config.ScalarTypes.Contains(type))
                return MapScalar(config, type, columns);
            return MapModel(config, tracker, Table.Get(type), columns);
        }

        private static object MapModel(MappingConfig config, ITracker tracker, Table table, ColumnValueSet columns)
        {
            object pk = null;

            if (table.PrimaryKey != null)
            {
                if (table.PrimaryKey.PhysicalColumns.All(x => columns[x.Name] == null))
                    return null;

                pk = table.PrimaryKey.FromQuery(columns, config);
                var cached = tracker.Get(table, pk);
                if (cached != null)
                    return cached;
            }

            var model = table.CreateNew();
            MapModel(config, model, table, columns);

            if (pk != null)
                tracker.Set(table, pk, model);

            return model;
        }

        public static void MapModel(MappingConfig config, object model, Table table, ColumnValueSet columns)
        {
            foreach (var column in table.LogicalColumns)
                table[model, column] = column.FromQuery(columns, config);
        }

        private static object MapScalar(MappingConfig config, Type type, ColumnValueSet columns)
        {
            return config.FromDb(type, columns.Single().Value);
        }

        public static List<object> ConsructObjectGraph(Table[] tables, List<object[]> rows)
        {
            if (!rows.Any())
                return new List<object>();
            return ConstructNode(new Range<Table>(tables), new Range2<object>(rows.ToArray(), rows.Count, rows[0].Length)).ToList();
        }

        private static IEnumerable<object> ConstructNode(Range<Table> tables, Range2<object> rows)
        {
            var table = tables[0];

            var relations = new List<(int index, Relation relation)>();
            for (int i = 1; i < tables.Length; ++i)
                if (table.Relations.TryGetValue(tables[i].Type, out Relation r))
                    relations.Add((i, r));
            relations.Add((tables.Length, null));

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
                if (done.Contains(roots[roi].pk))
                    continue;

                var root = rows[roots[roi].index, 0];

                for (var rei = 0; rei < relations.Count - 1; ++rei)
                {
                    var subts = tables.GetRange(relations[rei].index, relations[rei + 1].index - relations[rei].index);
                    var subrows = rows.GetRange(
                        roots[roi].index,
                        roots[roi + 1].index - roots[roi].index,
                        relations[rei].index,
                        relations[rei + 1].index - relations[rei].index);

                    var nodes = ConstructNode(subts, subrows).ToList();
                    relations[rei].relation.SetValue(root, nodes);

                    if (subts[0].Relations.TryGetValue(tables[0].Type, out Relation reverse))
                        foreach (var node in nodes)
                            reverse.SetValue(node, new List<object> { root });
                }

                yield return root;
                done.Add(roots[roi].pk);
            }
        }
    }
}
