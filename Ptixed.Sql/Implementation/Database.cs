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
        private IDisposable _result;

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
            _result?.Dispose();
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

        public int NonQuery(params Query[] queries)
        {
            if (queries.Length == 0)
                return 0;

            var query = Sql.Query.Join(Sql.Query.Separator, queries).ToSql(CreateCommand(), Config.Mappping);
            return query.ExecuteNonQuery();
        }

        public IEnumerable<T> Query<T>(Query query, params Type[] types)
        {
            var command = query.ToSql(CreateCommand(), Config.Mappping);

            var ret = new QueryResult<T>(Config.Mappping, command.ExecuteReader(), _transaction?.EntityTracker, types);
            _result = ret;
            return ret;
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
            var result = Query<T>(QueryBuilder.Insert(entities)).ToList();

            var table = Table.Get(typeof(T));

            if (table.AutoIncrementColumn != null)
                foreach (var (inserted, i) in result.Select((x, i) => (x, i)))
                    table[entities[i], table.AutoIncrementColumn.LogicalColumn] = table[inserted, table.AutoIncrementColumn.LogicalColumn];

            return entities.ToList();
        }

        public void Update(params object[] entities)
        {
            if (entities.Length == 0)
                return;
            NonQuery(QueryBuilder.Update(entities));
        }

        public void Delete(params object[] entities)
        {
            if (entities.Length == 0)
                return;
            NonQuery(QueryBuilder.Delete(entities));
        }

        public void Delete<T>(params object[] ids)
        {
            if (ids.Length == 0)
                return;
            NonQuery(QueryBuilder.Delete(typeof(T), ids));
        }

        #endregion

        private SqlCommand CreateCommand()
        {
            _result?.Dispose();
            return Diagnostics.LastCommand = new SqlCommand()
            {
                Connection = _connection.Value,
                Transaction = _transaction.SqlTransaction,
                CommandTimeout = (int)Config.CommandTimeout.TotalSeconds,
            };
        }
    }
}
