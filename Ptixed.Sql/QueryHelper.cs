using System.Collections.Generic;
using System.Linq;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Meta;

namespace Ptixed.Sql
{
    public static class QueryHelper
    {
        public static Query GetById<T>(params object[] ids)
        {
            if (ids == null || ids.Length == 0)
                return null;
            
            var table = Table.Get(typeof(T));

            var query = new Query();
            query.Append($"SELECT * FROM {table} WHERE ");
            query.Append($" OR ", ids.Select(id => table.GetPrimaryKeyCondition(id)));
            return query;
        }

        public static Query Insert<T>(params T[] entities)
        {
            if (entities == null || entities.Length == 0)
                return null;
            
            var table = Table.Get(typeof(T));
            var columns = table.PhysicalColumns.Except(new[] { table.AutoIncrementColumn }).ToList<PhysicalColumn>();

            var query = new Query();
            query.Append($"INSERT INTO {table} (");
            query.Append($", ", columns.Select(x => new Query($"{x}")));
            query.Append($") OUTPUT ");
            query.Append($", ", table.PhysicalColumns.Select(column => new Query($"INSERTED.{column}")));
            query.Append($"VALUES ");
            query.Append($", ", entities.Select(entity => 
            {
                var values = table.ToQuery(columns, entity)
                    .Select(column => new Query($"{column.Value}"))
                    .ToList();

                var q = new Query();
                q.Append($"(");
                q.Append($", ", values);
                q.Append($")");
                return q;
            }));
            return query;
        }


        public static Query Upsert<T>(Query searchCondition, T entity)
        {
            var table = Table.Get(typeof(T));
            var columns = table.PhysicalColumns.Except(new[] { table.AutoIncrementColumn }).ToList<PhysicalColumn>();
            var allcolumns = table.PhysicalColumns.ToList<PhysicalColumn>();
            var columnValues = table.ToQuery(columns, entity).ToArray();

            var query = new Query();
            // MERGE
            query.Append($"MERGE {table} AS [Target] \n");
            // USING
            query.Append($"USING (SELECT ");
            query.Append($", ", table.ToQuery(allcolumns, entity).Select(x => new Query($"{x} = {x.Value}")));
            query.Append($") AS [Source] \n");
            query.Append(searchCondition);

            // UPDATE
            query.Append($"\n WHEN MATCHED THEN \n");
            query.Append($" UPDATE SET \n");
            query.Append($", ", columnValues.Select(column => new Query($"[Target].{column} = {column.Value} \n")));

            // INSERT
            query.Append($" WHEN NOT MATCHED THEN \n");
            query.Append($" INSERT ( ");
            query.Append($", ", columns.Select(x => new Query($"{x}")));
            query.Append($") \n VALUES ( ");
            query.Append($", ", columnValues.Select(column => new Query($"{column.Value}")));
            query.Append($")");

            query.Append($"OUTPUT ");
            query.Append($", ", table.PhysicalColumns.Select(x => new Query($"INSERTED.{x}")));
            query.Append($";");

            return query;
        }

        public static Query Insert(string table, IDictionary<string, object> values)
        {
            if (table.Contains(']'))
                throw PtixedException.InvalidIdenitfier(table);
            foreach (var column in values.Keys)
                if (column.Contains(']'))
                    throw PtixedException.InvalidIdenitfier(column);

            var query = new Query();
            query.Append(Query.Unsafe($"INSERT INTO [{table}] ("));
            query.Append($", ", values.Select(x => Query.Unsafe($"[{x.Key}]")));
            query.Append($") VALUES (");
            query.Append($", ", values.Select(x => new Query($"{x.Value}")));
            query.Append($")\n\n");
            query.Append($"SELECT SCOPE_IDENTITY()");
            return query;
        }

        public static Query Update(params object[] entities)
        {
            if (entities == null || entities.Length == 0)
                return null;
            
            return Query.Join($"\n\n", entities.Select(entity =>
            {
                var table = Table.Get(entity.GetType());
                var columns = table.PhysicalColumns.Where(x => !x.IsPrimaryKey).ToList<PhysicalColumn>();

                var values = table.ToQuery(columns, entity)
                    .Select(column => new Query($"{column} = {column.Value}"))
                    .ToList();
                
                var query = new Query();
                query.Append($"UPDATE {table} SET ");
                query.Append($", ", values);
                query.Append($" WHERE ");
                query.Append(table.GetPrimaryKeyCondition(table[entity, table.PrimaryKey]));                
                return query;
            }));
        }

        public static Query Delete(params object[] entities)
        {
            if (entities == null || entities.Length == 0)
                return null;
            
            return Query.Join($"\n\n", entities.Select(entity =>
            {
                var table = Table.Get(entity.GetType());
                var id = table[entity, table.PrimaryKey];
                
                var query = new Query();
                query.Append($"DELETE FROM {table} WHERE ");
                query.Append(table.GetPrimaryKeyCondition(id));
                return query;
            }));
        }

        public static Query Delete<T>(params object[] ids)
        {
            if (ids == null || ids.Length == 0)
                return null;
            
            var table = Table.Get(typeof(T));

            var query = new Query();
            query.Append($"DELETE FROM {table} WHERE ");
            query.Append($" OR ", ids.Select(id => table.GetPrimaryKeyCondition(id)));
            return query;
        }
    }
}
