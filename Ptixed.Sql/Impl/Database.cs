using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Ptixed.Sql.Impl
{
    public abstract class Database<TConnection, TCommand, TParameter> : IDatabase<TParameter>
        where TConnection : DbConnection, new()
        where TParameter : DbParameter, new()
        where TCommand : DbCommand, new()
    {
        public readonly ConnectionConfig Config;
        public readonly DiagnosticsClass Diagnostics = new DiagnosticsClass();

        public MappingConfig MappingConfig => Config.Mappping;

        public class DiagnosticsClass
        {
            public SqlCommandWrapper<TConnection, TCommand> LastCommand;
        }

        protected Database(ConnectionConfig config)
        {
            Config = config;
        }
        
        protected TConnection Connect()
        {
            var connection = new TConnection();
            connection.ConnectionString = Config.ConnectionString;
            connection.Open();
            
            return connection;
        }

        private SqlCommandWrapper<TConnection, TCommand> CreateWrapperCommand(TConnection connection, DbTransaction transaction)
        {
            var command = new SqlCommandWrapper<TConnection, TCommand>(connection ?? Connect(), transaction, Config.CommandTimeout);
            Diagnostics.LastCommand = command;
            return command;
        }

        private int NonQueryInternal(TConnection connection, DbTransaction transaction, params Query<TParameter>[] query)
        {
            if (query.Length == 0)
                return 0;

            using (var wrapper = CreateWrapperCommand(connection, transaction))
            {
                var command = query.Aggregate((x, y) => x.Append($";\n\n").Append(y)).ToSql(wrapper.Command, Config.Mappping);
                return command.ExecuteNonQuery();
            }
        }
        public int NonQuery(params Query<TParameter>[] query) => NonQueryInternal(null, null, query);

        private IEnumerable<T> QueryInternal<T>(TConnection connection, DbTransaction transaction, Query<TParameter> query, params Type[] types)
        {
            using (var wrapper = CreateWrapperCommand(connection, transaction))
            {
                var command = query.ToSql(wrapper.Command, Config.Mappping);
                return new QueryResult<T>(Config.Mappping, command.ExecuteReader(), types);
            }
        }
        public IEnumerable<T> Query<T>(Query<TParameter> query, params Type[] types) => QueryInternal<T>(null, null, query, types);

        public IDatabaseTransaction<TParameter> OpenTransaction(IsolationLevel isolation)
        {
            return new DatabaseTransaction(this, isolation);
        }
        
        private class DatabaseTransaction : IDatabaseTransaction<TParameter>
        {
            private readonly Database<TConnection, TCommand, TParameter> _db;
            
            private readonly TConnection _connection;
            private readonly DbTransaction _transaction;
            
            private bool _commited;
            private bool _rolledback;

            public DatabaseTransaction(Database<TConnection, TCommand, TParameter> db, IsolationLevel isolation)
            {
                _db = db;
                _connection = db.Connect();
                try
                {
                    _transaction = _connection.BeginTransaction(isolation);
                }
                catch
                {
                    _connection.Dispose();
                    throw;
                }
            }

            public void Commit()
            {
                if (_rolledback)
                    throw PtixedException.InvalidTransacionState("rolled back");
                if (!_commited)
                    _transaction.Commit();
                _commited = true;
            }

            public void Dispose()
            {
                if (!_commited && !_rolledback)
                {
                    try { _transaction?.Rollback(); }
                    catch { /* don't care */ }
                    _rolledback = true;
                }

                _transaction?.Dispose();
                _connection?.Dispose();
            }

            IEnumerable<T> IDatabaseAccessor<TParameter>.Query<T>(Query<TParameter> query, params Type[] types)
            {
                return _db.QueryInternal<T>(_connection, _transaction, query, types);
            }

            int IDatabaseAccessor<TParameter>.NonQuery(params Query<TParameter>[] query)
            {
                return _db.NonQueryInternal(_connection, _transaction, query);
            }
        }
    }
}
