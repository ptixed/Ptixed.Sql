using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace Ptixed.Sql.Impl
{
    /// <summary>
    /// This class is not thread safe
    /// </summary>
    public abstract class Database<TCommand, TParameter> : IDatabase<TParameter>
        where TParameter : DbParameter, new()
        where TCommand : DbCommand, new()
    {
        private DbTransaction _transaction;
        private IDisposable _result;

        public readonly ConnectionConfig Config;
        public Lazy<DbConnection> Connection;

        public readonly DiagnosticsClass Diagnostics = new DiagnosticsClass();

        public MappingConfig MappingConfig => Config.Mappping;

        public class DiagnosticsClass
        {
            public TCommand LastCommand;
        }

        public Database(ConnectionConfig config)
        {
            Config = config;
            Reset();
        }

        private TCommand NewCommand()
        {
            try { _result?.Dispose(); }
            catch { /* don't care */ }
            
            var command = new TCommand()
            {
                Connection = Connection.Value,
                Transaction = _transaction,
                CommandTimeout = (int)Config.CommandTimeout.TotalSeconds,
            };
            Diagnostics.LastCommand = command;
            return command;
        }

        public void Dispose()
        {
            try { _result?.Dispose(); }
            catch { /* don't care */ }

            try { _transaction?.Dispose(); }
            catch { /* don't care */ }

            if (Connection?.IsValueCreated == true)
                try { Connection.Value.Dispose(); }
                catch { /* don't care */ }
        }

        public void Reset()
        {
            Dispose();
            Connection = new Lazy<DbConnection>(() =>
            {
                var sql = CreateConnection(Config.ConnectionString);
                sql.Open();
                return sql;
            }, LazyThreadSafetyMode.None);
        }

        protected abstract DbConnection CreateConnection(string connectionString);

        public int NonQuery(params Query<TParameter>[] query)
        {
            if (query.Length == 0)
                return 0;

            var command = query.Aggregate((x, y) => x.Append($";\n\n").Append(y)).ToSql(NewCommand(), Config.Mappping);
            return command.ExecuteNonQuery();
        }

        public IEnumerable<T> Query<T>(Query<TParameter> query, params Type[] types)
        {
            var command = query.ToSql(NewCommand(), Config.Mappping);

            var ret = new QueryResult<T>(Config.Mappping, command.ExecuteReader(), types);
            _result = ret;
            return ret;
        }

        public IDatabaseTransaction OpenTransaction(IsolationLevel isolation)
        {
            if (_transaction != null)
                throw PtixedException.InvalidTransacionState("open"); 
            return new DatabaseTransaction(this, isolation);
        }
        
        private class DatabaseTransaction : IDatabaseTransaction
        {
            private readonly Database<TCommand, TParameter> _db;
            private bool _commited;
            private bool _rolledback;

            public DatabaseTransaction(Database<TCommand, TParameter> db, IsolationLevel isolation)
            {
                _db = db;
                _db._transaction = db.Connection.Value.BeginTransaction(isolation);
            }

            public void Commit()
            {
                if (_rolledback)
                    throw PtixedException.InvalidTransacionState("rolled back");
                if (!_commited)
                    _db._transaction.Commit();
                _commited = true;
            }

            public void Dispose()
            {
                if (!_commited)
                {
                    try { _db._transaction?.Rollback(); }
                    catch { /* don't care */ }
                    _rolledback = true;
                }

                try { _db._transaction?.Dispose(); }
                catch { /* don't care */ }
                
                _db._transaction = null;
            }
        }
    }
}
