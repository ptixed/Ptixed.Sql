using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Metadata;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Ptixed.Sql.Implementation
{
    internal class DatabaseTransaction : QueryExecutor, IDatabaseTransaction
    {
        public delegate void Disposed();
        public event Disposed OnDisposed;
        
        public readonly SqlTransaction SqlTransaction;
        public readonly TransactionalTracker Tracker = new TransactionalTracker();

        private bool? _commited;
        private bool _disposed;

        public DatabaseTransaction(IDatabase db, SqlTransaction transaction) : base(db)
        {
            SqlTransaction = transaction;
        }

        public void Commit()
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");
            SqlTransaction.Commit();
            _commited = true;
        }

        public void Dispose()
        {
            if (!_commited.HasValue)
            {
                SqlTransaction.Rollback();
                _commited = false;
            }
            if (!_disposed)
            {
                _disposed = true;
                OnDisposed?.Invoke();
                SqlTransaction.Dispose();
            }
        }

        private void CheckStatus()
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");
        }

        #region QueryExecutor

        public override IEnumerable<T> Query<T>(Query query, params Table[] tables)
        {
            CheckStatus();

            var queries = Tracker.Flush();
            queries.Add(query);
            return base.Query<T>(Sql.Query.Join(Sql.Query.Separator, queries), tables);
        }

        public override int NonQuery(IEnumerable<Query> queries)
        {
            CheckStatus();

            return base.NonQuery(Tracker.Flush().Concat(queries));
        }

        public override List<T> GetById<T>(IEnumerable<object> ids)
        {
            var result = new List<T>();
            var missing = new List<object>();

            foreach (var id in ids)
            {
                var cached = Tracker.Get(typeof(T), id);
                if (cached == null)
                    missing.Add(id);
                else
                    result.Add((T)cached);
            }

            if (missing.Any())
                result.AddRange(base.GetById<T>(missing));

            return result;
        }

        public override void Insert(IEnumerable<object> entities)
        {
            base.Insert(entities);
        }

        public override void Update(IEnumerable<object> entities)
        {
            foreach (var entity in entities)
                Tracker.ScheduleUpdate(entity);
        }

        public override void Delete(IEnumerable<(Table table, object id)> keys)
        {
            foreach (var (table, id) in keys)
                Tracker.ScheduleDelete(table, id);
        }

        #endregion
    }
}
