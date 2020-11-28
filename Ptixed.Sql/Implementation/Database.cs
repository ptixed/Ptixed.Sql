using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace Ptixed.Sql.Implementation
{
    public class Database : IDatabase
    {
        private Lazy<SqlConnection> _connection;
        private DatabaseTransaction _transaction;

        private IQueryExecutor Executor => _transaction ?? DefaultQueryExecutor;
        private IQueryExecutor DefaultQueryExecutor;

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
                Transaction = _transaction.SqlTransaction,
                CommandTimeout = (int)Config.CommandTimeout.TotalSeconds,
            };
        }

        public int NonQuery(params Query[] queries) => Executor.NonQuery(queries);
        public IEnumerable<T> Query<T>(Query query, params Type[] types) => Executor.Query<T>(query, types);

        public T Insert<T>(T entity) => Insert(new[] { entity })[0];
        public List<T> Insert<T>(params T[] entities) => Executor.Insert(entities);
        public void Update(params object[] entities) => Executor.Update(entities);
        public void Delete(params object[] entities)
        {
            Executor.Delete(entities.Select(x =>
            {
                var table = Table.Get(x.GetType());
                return (table, table[x, table.PrimaryKey]);
            }));
        }
        public void Delete<T>(params object[] ids)
        {
            var table = Table.Get(typeof(T));
            Executor.Delete(ids.Select(x => (table, x)));
        }
    }
}
