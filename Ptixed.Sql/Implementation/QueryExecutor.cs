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

        public virtual IEnumerable<T> Query<T>(Query query, params Table[] tables)
        {
            if (query.IsEmpty)
                return Enumerable.Empty<T>();
            var command = query.ToSql(_db.CreateCommand(), _db.Config.Mappping);
            return new QueryResult<T>(_db.Config.Mappping, command.ExecuteReader(), new DefaultTracker(), tables);
        }

        public virtual int NonQuery(IEnumerable<Query> queries)
        {
            var query = Sql.Query.Join(Sql.Query.Separator, queries);
            if (query.IsEmpty)
                return 0;
            return query.ToSql(_db.CreateCommand(), _db.Config.Mappping).ExecuteNonQuery();
        }

        public virtual List<T> GetById<T>(IEnumerable<object> ids)
        {
            return Query<T>(QueryBuilder.GetById<T>(ids), typeof(T)).ToList();
        }

        public virtual void Insert(IEnumerable<object> entities)
        {
            Type last = null;
            var batch = new List<object>();

            void ExecuteBatch()
            {
                var table = Table.Get(last);                
                var result = Query<object>(QueryBuilder.Insert(entities), last);
                foreach (var (entity, created) in batch.Zip(result, (x, y) => (x, y)))
                    table.Copy(created, entity); // todo: map directly to destination object instead of making a copy
                batch = new List<object>();
            }

            foreach (var entity in entities)
            {
                if (last == null || last == entity.GetType())
                    batch.Add(entity);
                else
                    ExecuteBatch();
                last = entity.GetType();
            }
            if (batch.Count > 0)
                ExecuteBatch();
        }

        public virtual void Update(IEnumerable<object> entities)
        {
            NonQuery(new[] { QueryBuilder.Update(entities) });
        }

        public virtual void Delete(IEnumerable<(Table table, object id)> deletes)
        {
            NonQuery(new[] { QueryBuilder.Delete(deletes) });
        }
    }
}
