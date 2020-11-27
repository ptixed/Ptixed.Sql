using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Ptixed.Sql.Implementation
{
    internal class DatabaseTransaction : IDatabaseTransaction
    {
        public delegate void Disposed();
        public event Disposed OnDisposed;

        private readonly IQueryExecutor _db;
        public readonly SqlTransaction SqlTransaction;
        public readonly TransactionalTracker EntityTracker = new TransactionalTracker();

        private bool? _commited;
        private bool _disposed;

        public DatabaseTransaction(IQueryExecutor db, SqlTransaction transaction)
        {
            _db = db;
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
            }
        }

        public int NonQuery(params Query[] queries)
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");

            var qs = new List<Query>();
            qs.Add(EntityTracker.PrepareChangesQuery());
            qs.AddRange(queries);
            return _db.NonQuery(qs.ToArray());
        }

        public IEnumerable<T> Query<T>(Query query, params Type[] types)
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");

            var changes = EntityTracker.PrepareChangesQuery();
            changes.Append(Sql.Query.Separator);
            changes.Append(query);

            // keep object copies 
            // first i must fix map so it won't produce copies

            // var entities = _db.Query<T>(changes, types);

            throw new NotImplementedException();
        }

        #region Queries

        public T Insert<T>(T entity)
        {
            return Insert(new[] { entity })[0];
        }

        public List<T> Insert<T>(params T[] entities)
        {
            if (entities.Length == 0)
                return new List<T>();

            // PrepareChangesQuery() + Database.Insert + tracker

            // 

            throw new NotImplementedException();
        }

        public void Update(params object[] entities)
        {
            // PrepareChangesQuery() +  Database.Update

            throw new NotImplementedException();
        }

        public void Delete(params object[] entities)
        {
            foreach (var entity in entities)
            {
                var table = Table.Get(entity.GetType());
                EntityTracker.ScheduleDelete(table, table[entity, table.PrimaryKey]);
            }
        }

        public void Delete<T>(params object[] ids)
        {
            var table = Table.Get(typeof(T));
            foreach (var id in ids)
                EntityTracker.ScheduleDelete(table, id);
        }

        #endregion
    }
}
