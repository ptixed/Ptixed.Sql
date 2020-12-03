using System.Collections.Generic;
using System.Linq;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Implementation
{
    internal static class QueryBuilder
    {
        public static Query GetById<T>(IEnumerable<object> ids)
        {
            var table = Table.Get(typeof(T));

            var condition = Query.Join(new Query($" OR "), ids.Select(id => table.GetPrimaryKeyCondition(id)));

            if (condition.IsEmpty)
                return condition;

            var query = new Query();
            query.Append($"SELECT * FROM {table} WHERE ");
            query.Append(condition);
            return query;
        }

        public static Query Insert<T>(IEnumerable<T> entities)
        {
            var table = Table.Get(typeof(T));
            var columns = table.PhysicalColumns.Except(new[] { table.AutoIncrementColumn }).ToList<PhysicalColumn>();

            var inserts = Query.Join(new Query($", "), entities.Select(entity =>
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

            if (inserts.IsEmpty)
                return inserts;

            var query = new Query();
            query.Append($"INSERT INTO {table} (");
            query.Append($", ", columns.Select(x => new Query($"{x}")));
            query.Append($") OUTPUT ");
            query.Append($", ", table.PhysicalColumns.Select(column => new Query($"INSERTED.{column}")));
            query.Append($"VALUES ");
            query.Append(inserts);
            return query;
        }

        public static Query Update(IEnumerable<object> entities)
        {
            return Query.Join(Query.Separator, entities.Select(entity =>
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

        public static Query Delete(IEnumerable<(Table table, object id)> deletes)
        {
            return Query.Join(Query.Separator, deletes.Select(x =>
            {
                var condition = x.table.GetPrimaryKeyCondition(x.id);
                return new Query($"DELETE FROM {x.table} WHERE ").Append(condition);
            }));
        }
    }
}
