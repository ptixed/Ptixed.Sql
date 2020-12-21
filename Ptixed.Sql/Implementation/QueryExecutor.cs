using Ptixed.Sql.Collections;
using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql.Implementation
{
    internal class QueryExecutor : IQueryExecutor
    {
        protected readonly IDatabase _db;

        protected virtual ITracker Tracker => new DefaultTracker();

        public QueryExecutor(IDatabase db)
        {
            _db = db;
        }

        public virtual IEnumerable<T> Query<T>(Query query, params Type[] types)
        {
            var command = query.ToSql(_db.CreateCommand(), _db.Config.Mappping);
            if (types.Length == 0)
                types = new[] { typeof(T) };
            return new QueryResult<T>(_db.Config.Mappping, command.ExecuteReader(), Tracker, types);
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

        public virtual void Insert<T>(IEnumerable<T> entities)
        {
            var items = entities.ToList();
            var table = Table.Get(typeof(T));
            var query = Sql.Query.Join(Sql.Query.Separator, items.Select(x => QueryBuilder.Insert(x)));
            var command = query.ToSql(_db.CreateCommand(), _db.Config.Mappping);

            using (var reader = command.ExecuteReader())
                for (var i = 0; i < items.Count; ++i)
                {
                    reader.Read();
                    ModelMapper.MapModel(_db.Config.Mappping, items[i], table, new ColumnValueSet(reader));
                    if (table.PrimaryKey != null)
                        Tracker.Set(table, table[items[i], table.PrimaryKey], items[i]);
                }
        }

        public virtual void Update(IEnumerable<object> entities)
        {
            NonQuery(entities.Select(x => QueryBuilder.Update(x)));
        }

        public virtual void Delete(IEnumerable<(Table table, object id)> deletes)
        {
            NonQuery(deletes.Select(x => QueryBuilder.Delete(x.table, x.id)));
        }
    }
}
