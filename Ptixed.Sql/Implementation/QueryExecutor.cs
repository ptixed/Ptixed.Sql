using Ptixed.Sql.Collections;
using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ptixed.Sql.Implementation.Transactions
{
    internal class QueryExecutor : IQueryExecutor
    {
        private readonly IDatabaseCore _db;
        private readonly ITracker _tracker;

        public QueryExecutor(IDatabaseCore db, ITracker tracker)
        {
            _db = db;
            _tracker = tracker;
        }

        public IEnumerator<T> LazyQuery<T>(Query query, params Type[] types)
        {
            var command = query.ToSql(_db.CreateCommand(), _db.Config.Mappping);
            if (types.Length == 0)
                types = new[] { typeof(T) };
            return new QueryResult<T>(_db.Config.Mappping, command.ExecuteReader(), _tracker, types).GetEnumerator();
        }

        public List<T> Query<T>(Query query, params Type[] types)
        {
            using (var result = LazyQuery<T>(query, types))
            {
                var ret = new List<T>();
                while (result.MoveNext())
                    ret.Add(result.Current);
                return ret;
            }
        }

        public int NonQuery(IEnumerable<Query> queries)
        {
            var query = Sql.Query.Join(Sql.Query.Separator, queries);
            return query.ToSql(_db.CreateCommand(), _db.Config.Mappping).ExecuteNonQuery();
        }

        public List<T> GetById<T>(IEnumerable<object> ids)
        {
            return Query<T>(QueryBuilder.GetById<T>(ids), typeof(T)).ToList();
        }

        public void Insert<T>(IEnumerable<T> entities)
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
                }
        }

        public void Update(IEnumerable<object> entities)
        {
            NonQuery(entities.Select(x => QueryBuilder.Update(x)));
        }

        public void Delete(IEnumerable<(Table table, object id)> deletes)
        {
            NonQuery(deletes.Select(x => QueryBuilder.Delete(x.table, x.id)));
        }
    }
}
