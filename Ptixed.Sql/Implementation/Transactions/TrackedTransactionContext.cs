using Ptixed.Sql.Collections;
using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Ptixed.Sql.Implementation.Transactions
{
    internal class TrackedTransactionContext : TransactionContextBase, ITransactionContext
    {
        private readonly TransactionalTracker _tracker = new TransactionalTracker();
        private readonly IDatabaseCore _db;

        private readonly IQueryExecutor _executor; 

        public TrackedTransactionContext(IDatabaseCore db, SqlTransaction transaction) : base(transaction)
        {
            _db = db;
            _executor = new QueryExecutor(db, _tracker);
        }

        public override void Commit()
        {
            var queries = _tracker.Flush();
            if (queries.Any())
                NonQuery(queries);

            base.Commit();
        }

        public IEnumerator<T> LazyQuery<T>(Query query, params Type[] types)
        {
            return _executor.LazyQuery<T>(query, types);
        }

        public List<T> Query<T>(Query query, params Type[] types)
        {
            return _executor.Query<T>(query, types);
        }

        public int NonQuery(IEnumerable<Query> queries)
        {
            return _executor.NonQuery(_tracker.Flush().Concat(queries));
        }

        public List<T> GetById<T>(IEnumerable<object> ids)
        {
            var result = new List<T>();
            var missing = new List<object>();

            foreach (var id in ids)
            {
                var cached = _tracker.Get(typeof(T), id);
                if (cached == null)
                    missing.Add(id);
                else
                    result.Add((T)cached);
            }

            if (missing.Any())
                result.AddRange(_executor.GetById<T>(missing));

            return result;
        }

        public void Insert<T>(IEnumerable<T> entities)
        {
            var items = entities.ToList();
            var table = Table.Get(typeof(T));

            var queries = _tracker.Flush();
            queries.AddRange(items.Select(x => QueryBuilder.Insert(x)));

            var query = Sql.Query.Join(Sql.Query.Separator, queries);
            var command = query.ToSql(_db.CreateCommand(), _db.Config.Mappping);

            using (var reader = command.ExecuteReader())
                for (var i = 0; i < items.Count; ++i)
                {
                    var item = items[i];
                    reader.Read();

                    ModelMapper.MapModel(_db.Config.Mappping, item, table, new ColumnValueSet(reader));
                    _tracker.Set(table, table[item, table.PrimaryKey], item);
                }
        }

        public void Update(IEnumerable<object> entities)
        {
            foreach (var entity in entities)
                _tracker.ScheduleUpdate(entity);
        }

        public void Delete(IEnumerable<(Table table, object id)> keys)
        {
            foreach (var (table, id) in keys)
                _tracker.ScheduleDelete(table, id);
        }
    }
}
