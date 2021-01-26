using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Implementation.Transactions;
using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace Ptixed.Sql.Implementation
{
    public class Database : IDatabase
    {
        private Lazy<SqlConnection> _connection;
        private ITransactionContext _transaction;

        private IQueryExecutor Transaction
        {
            get
            {
                if (_transaction == null || _transaction.IsDisposed)
                    return new QueryExecutor(this, new DefaultTracker());
                return _transaction;
            }
        }

        public DatabaseConfig Config { get; }
        public DatabaseDiagnostics Diagnostics { get; } = new DatabaseDiagnostics();

        public Database(DatabaseConfig config)
        {
            Config = config;
            ResetConnection();
        }

        public void ResetConnection()
        {
            Dispose();
            _connection = new Lazy<SqlConnection>(() =>
            {
                var sql = new SqlConnection(Config.ConnectionString);
                sql.Open();
                return sql;
            }, LazyThreadSafetyMode.None);
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            if (_connection?.IsValueCreated == true)
                _connection.Value.Dispose();
        }

        public ITransactionContext OpenTransaction(IsolationLevel? isolation = null, bool tracking = false)
        {
            if (_transaction != null && _transaction.IsDisposed != true)
                throw PtixedException.InvalidTransacionState("open");

            var sqltransaction = _connection.Value.BeginTransaction(isolation ?? Config.DefaultIsolationLevel);
            if (tracking)
                _transaction = new TrackedTransactionContext(this, sqltransaction);
            else
                _transaction = new UntrackedTransactionContext(this, sqltransaction);
            return _transaction;
        }

        public SqlCommand CreateCommand()
        {
            return Diagnostics.LastCommand = new SqlCommand()
            {
                Connection = _connection.Value,
                Transaction = _transaction?.SqlTransaction,
                CommandTimeout = (int)Config.CommandTimeout.TotalSeconds,
            };
        }

        public List<T> Query<T>(Query query, params Type[] types) => Transaction.Query<T>(query, types);
        public IEnumerator<T> LazyQuery<T>(Query query, params Type[] types) => Transaction.LazyQuery<T>(query, types);
        public int NonQuery(IEnumerable<Query> queries) => Transaction.NonQuery(queries);

        public List<T> GetById<T>(IEnumerable<object> ids) => Transaction.GetById<T>(ids);
        public void Insert<T>(IEnumerable<T> entities) => Transaction.Insert(entities);
        public void Update(IEnumerable<object> entities) => Transaction.Update(entities);
        public void Delete(IEnumerable<(Table table, object id)> deletes) => Transaction.Delete(deletes);
    }
}
