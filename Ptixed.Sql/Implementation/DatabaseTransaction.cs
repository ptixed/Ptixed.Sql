using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Metadata;
using System;
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
        public readonly TransactionalTracker EntityTracker = new TransactionalTracker();

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

        public override int NonQuery(IEnumerable<Query> queries)
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");

            return base.NonQuery(EntityTracker.Flush().Concat(queries));
        }

        public IEnumerable<T> Query<T>(Query query, params Type[] types)
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");

            var queries = EntityTracker.Flush();
            queries.Add(query);            
            return base.Query<T>(Sql.Query.Join(Sql.Query.Separator, queries), types);
        }

        public override List<T> Insert<T>(params T[] entities)
        {
            return base.Insert(entities);
        }

        public override void Update(IEnumerable<object> entities)
        {
            foreach (var entity in entities)
                EntityTracker.ScheduleUpdate(entity);
        }

        public override void Delete(IEnumerable<(Table table, object id)> keys)
        {
            foreach (var (table, id) in keys)
                EntityTracker.ScheduleDelete(table, id);
        }
    }
}
