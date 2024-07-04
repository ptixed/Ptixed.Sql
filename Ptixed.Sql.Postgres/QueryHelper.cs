using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Meta;

namespace Ptixed.Sql.Postgres
{
    public static class QueryHelper
    {
        public static Query GetPrimaryKeyCondition(Table table, object key)
        {
            var query = new Query();
            query.Append($"(");
            query.Append($" AND ", table.PrimaryKey.FromValueToQuery(key).Select(column => new Query($"{column} = {column.Value}")));
            query.Append($")");
            return query;
        }

        public static Query GetById<T>(params object[] ids)
        {
            if (ids == null || ids.Length == 0)
                return null;
            
            var table = Table.Get(typeof(T));

            var query = new Query();
            query.Append($"SELECT * FROM {table} WHERE ");
            query.Append($" OR ", ids.Select(id => GetPrimaryKeyCondition(table, id)));
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
            query.Append($") VALUES ");
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
            query.Append($" RETURNING ");
            query.Append($", ", table.PhysicalColumns.Select(column => new Query($"{column}")));
            return query;
        }

        public static Query Update(params object[] entities)
        {
            if (entities == null || entities.Length == 0)
                return null;

            var query = new Query();
            for (var i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                var table = Table.Get(entity.GetType());
                var columns = table.PhysicalColumns.Where(x => !x.IsPrimaryKey).ToList<PhysicalColumn>();

                var values = table.ToQuery(columns, entity)
                    .Select(column => new Query($"{column} = {column.Value}"))
                    .ToList();

                query.Append($"UPDATE {table} SET ");
                query.Append($", ", values);
                query.Append($" WHERE ");
                query.Append(GetPrimaryKeyCondition(table, table[entity, table.PrimaryKey]));

                if (i < entities.Length - 1)
                    query.Append($";");
            }
            return query;
        }

        public static Query Delete(params object[] entities)
        {
            if (entities == null || entities.Length == 0)
                return null;
            
            var query = new Query();
            for (var i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                var table = Table.Get(entity.GetType());
                var id = table[entity, table.PrimaryKey];

                query.Append($"DELETE FROM {table} WHERE ");
                query.Append(GetPrimaryKeyCondition(table, id));

                if (i < entities.Length - 1)
                    query.Append($";");
            }
            return query;
        }

        public static Query Delete<T>(params object[] ids)
        {
            if (ids == null || ids.Length == 0)
                return null;
            
            var table = Table.Get(typeof(T));

            var query = new Query();
            query.Append($"DELETE FROM {table} WHERE ");
            query.Append($" OR ", ids.Select(id => GetPrimaryKeyCondition(table, id)));
            return query;
        }
    }
}
