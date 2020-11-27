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

        private readonly List<object> _deletes;

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

        public int NonQuery(params Query[] query)
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");

            var qs = new List<Query>();
            qs.Add(PrepareChangesQuery());
            qs.AddRange(query);
            return _db.NonQuery(qs.ToArray());
        }

        public IEnumerable<T> Query<T>(Query query, params Type[] types)
        {
            if (_commited.HasValue)
                throw PtixedException.InvalidTransacionState(_commited.Value ? "committed" : "rolledback");

            var changes = PrepareChangesQuery();
            changes.Append($"\n\n");
            changes.Append(query);

            // keep object copies 
            // first i must fix map so it won't produce copies

            // var entities = _db.Query<T>(changes, types);

            throw new NotImplementedException();
        }

        public List<T> Insert<T>(params T[] entities)
        {
            if (entities.Length == 0)
                return new List<T>();

            // PrepareChangesQuery() +  Database.Insert

            throw new NotImplementedException();
        }

        public void Delete(params object[] entities)
        {
            _deletes.AddRange(entities);
        }

        private Query PrepareChangesQuery()
        {
            // prepare updates and deletes queries

            _deletes.Clear();

            // commit tracked objects 

            throw new NotImplementedException();
        }
    }
}
