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
            var query = new Query();
            query.Append($"SELECT * FROM {table} WHERE ");
            query.Append($" OR ", ids.Select(id => table.GetPrimaryKeyCondition(id)));
            return query;
        }

        public static Query Insert(object entity)
        {
            var table = Table.Get(entity.GetType());
            var columns = table.PhysicalColumns.Except(new[] { table.AutoIncrementColumn }).ToList<PhysicalColumn>();

            var query = new Query();
            query.Append($"INSERT INTO {table} (");
            query.Append($", ", columns.Select(x => new Query($"{x}")));
            query.Append($") OUTPUT INSERTED.* VALUES (");
            query.Append($", ", table.ToQuery(columns, entity).Select(column => new Query($"{column.Value}")));
            query.Append($")");
            return query;
        }

        public static Query Update(object entity)
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
        }

        public static Query Delete(Table table, object id)
        {
            var condition = table.GetPrimaryKeyCondition(id);
            return new Query($"DELETE FROM {table} WHERE ").Append(condition);
        }
    }
}
