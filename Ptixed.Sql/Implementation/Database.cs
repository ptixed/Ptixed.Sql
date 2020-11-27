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

        public int NonQuery(params Query[] query)
        {
            if (query.Length == 0)
                return 0;

            var command = query.Aggregate((x, y) => x.Append($"\n\n").Append(y)).ToSql(CreateCommand(), Config.Mappping);
            return command.ExecuteNonQuery();
        }

        public IEnumerable<T> Query<T>(Query query, params Type[] types)
        {
            var command = query.ToSql(CreateCommand(), Config.Mappping);

            var ret = new QueryResult<T>(Config.Mappping, command.ExecuteReader(), types);
            _result = ret;
            return ret;
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

        public void Delete(params object[] entities)
        {
            NonQuery(QueryBuilder.Delete(entities));
        }

        private SqlCommand CreateCommand()
        {
            _result?.Dispose();
            var command = new SqlCommand()
            {
                Connection = _connection.Value,
                Transaction = _transaction.SqlTransaction,
                CommandTimeout = (int)Config.CommandTimeout.TotalSeconds,
            };
            Diagnostics.LastCommand = command;
            return command;
        }
    }
}
