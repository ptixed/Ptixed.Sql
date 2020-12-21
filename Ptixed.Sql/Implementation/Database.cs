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
        private DatabaseTransaction _transaction;

        private IQueryExecutor Executor => _transaction ?? DefaultQueryExecutor;
        private readonly IQueryExecutor DefaultQueryExecutor;

        public DatabaseConfig Config { get; }
        public DatabaseDiagnostics Diagnostics { get; } = new DatabaseDiagnostics();

        public Database(DatabaseConfig config)
        {
            Config = config;
            DefaultQueryExecutor = new QueryExecutor(this);
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

        public IDatabaseTransaction OpenTransaction(IsolationLevel? isolation = null)
        {
            if (_transaction != null)
                throw PtixedException.InvalidTransacionState("open");
            
            var sqltransaction = _connection.Value.BeginTransaction(isolation ?? Config.DefaultIsolationLevel);
            _transaction = new DatabaseTransaction(this, sqltransaction);
            _transaction.OnDisposed += () => _transaction = null;
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

        public IEnumerable<T> Query<T>(Query query, params Type[] types) => Executor.Query<T>(query, types);
        public int NonQuery(IEnumerable<Query> queries) => Executor.NonQuery(queries);

        public List<T> GetById<T>(IEnumerable<object> ids) => Executor.GetById<T>(ids);
        public void Insert<T>(IEnumerable<T> entities) => Executor.Insert(entities);
        public void Update(IEnumerable<object> entities) => Executor.Update(entities);
        public void Delete(IEnumerable<(Table table, object id)> deletes) => Executor.Delete(deletes);
    }
}
