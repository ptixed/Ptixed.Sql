using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql.Implementation
{
    internal class QueryExecutor : IQueryExecutor
    {
        private readonly IDatabase _db;

        public QueryExecutor(IDatabase db)
        {
            _db = db;
        }

        public virtual int NonQuery(IEnumerable<Query> queries)
        {
            if (!queries.Any())
                return 0;

            var query = Sql.Query.Join(Sql.Query.Separator, queries).ToSql(_db.CreateCommand(), _db.Config.Mappping);
            return query.ExecuteNonQuery();
        }

        public virtual QueryResult<T> Query<T>(Query query, IEnumerable<Type> types)
        {
            var command = query.ToSql(_db.CreateCommand(), _db.Config.Mappping);
            return new QueryResult<T>(_db.Config.Mappping, command.ExecuteReader(), new DefaultTracker(), types.ToArray());
        }

        public virtual List<T> Insert<T>(params T[] entities)
        {
            if (entities.Length == 0)
                return new List<T>();

            var result = Query<T>(QueryBuilder.Insert(entities), new[] { typeof(T) }).ToList();
            var table = Table.Get(typeof(T));

            if (table.AutoIncrementColumn != null)
                foreach (var (inserted, i) in result.Select((x, i) => (x, i)))
                    table[entities[i], table.AutoIncrementColumn.LogicalColumn] = table[inserted, table.AutoIncrementColumn.LogicalColumn];

            return entities.ToList();
        }

        public virtual void Update(IEnumerable<object> entities)
        {
            if (!entities.Any())
                return;
            NonQuery(new[] { QueryBuilder.Update(entities) });
        }

        public virtual void Delete(IEnumerable<(Table table, object id)> keys)
        {
            NonQuery(keys.Select(x => QueryBuilder.Delete(x.table, x.id)));
        }
    }
}
