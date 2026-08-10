using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace Ptixed.Sql.Impl
{
    public abstract class Database<TConnection, TCommand, TParameter> : IDatabase<TParameter>
        where TConnection : DbConnection, new()
        where TParameter : DbParameter, new()
        where TCommand : DbCommand, new()
    {
        private (TConnection Connection, DbTransaction Transaction)? _transaction;

        public readonly ConnectionConfig Config;

        public readonly DiagnosticsClass Diagnostics = new DiagnosticsClass();

        public MappingConfig MappingConfig => Config.Mappping;

        public class DiagnosticsClass
        {
            public SqlCommandWrapper<TConnection, TCommand> LastCommand;
        }

        public Database(ConnectionConfig config)
        {
            Config = config;
        }
        
        protected TConnection Connect()
        {
            if (_transaction != null)
                return _transaction.Value.Connection;
            
            var connection = new TConnection();
            connection.ConnectionString = Config.ConnectionString;
            connection.Open();
            
            return connection;
        }

        private SqlCommandWrapper<TConnection, TCommand> CreateWrapperCommand()
        {
            var command = new SqlCommandWrapper<TConnection, TCommand>(_transaction?.Connection ?? Connect(), _transaction?.Transaction, Config.CommandTimeout);
            Diagnostics.LastCommand = command;
            return command;
        }

        public int NonQuery(params Query<TParameter>[] query)
        {
            if (query.Length == 0)
                return 0;

            using (var wrapper = CreateWrapperCommand())
            {
                var command = query.Aggregate((x, y) => x.Append($";\n\n").Append(y)).ToSql(wrapper.Command, Config.Mappping);
                return command.ExecuteNonQuery();
            }
        }

        public IEnumerable<T> Query<T>(Query<TParameter> query, params Type[] types)
        {
            using (var wrapper = CreateWrapperCommand())
            {
                var command = query.ToSql(wrapper.Command, Config.Mappping);
                return new QueryResult<T>(Config.Mappping, command.ExecuteReader(), types);
            }
        }

        public IDatabaseTransaction OpenTransaction(IsolationLevel isolation)
        {
            if (_transaction != null)
                throw PtixedException.InvalidTransacionState("open"); 
            return new DatabaseTransaction(this, isolation);
        }
        
        private class DatabaseTransaction : IDatabaseTransaction
        {
            private readonly Database<TConnection, TCommand, TParameter> _db;
            private bool _commited;
            private bool _rolledback;

            public DatabaseTransaction(Database<TConnection, TCommand, TParameter> db, IsolationLevel isolation)
            {
                _db = db;
                var connection = db.Connect();
                _db._transaction = (connection, connection.BeginTransaction(isolation));
            }

            public void Commit()
            {
                if (_rolledback)
                    throw PtixedException.InvalidTransacionState("rolled back");
                if (!_commited)
                    _db._transaction.Value.Transaction.Commit();
                _commited = true;
            }

            public void Dispose()
            {
                if (!_commited)
                {
                    try { _db._transaction?.Transaction?.Rollback(); }
                    catch { /* don't care */ }
                    _rolledback = true;
                }

                try { _db._transaction?.Transaction?.Dispose(); }
                catch { /* don't care */ }

                try { _db._transaction?.Connection?.Dispose(); }
                catch { /* don't care */ }
                
                _db._transaction = null;
            }
        }
    }
}
