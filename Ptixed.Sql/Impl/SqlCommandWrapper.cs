using System;
using System.Data.Common;

namespace Ptixed.Sql.Impl
{
    public class SqlCommandWrapper<TConnection, TCommand> : IDisposable
        where TConnection : DbConnection, new()
        where TCommand : DbCommand, new()
    {
        private readonly DbTransaction _transaction;
        private readonly TConnection _connection;
        public readonly TCommand Command;
    
        public SqlCommandWrapper(TConnection connection, DbTransaction transaction, TimeSpan timeout)
        {
            _connection = connection;
            _transaction = transaction;
            Command = new TCommand()
            {
                Connection = connection,
                Transaction = _transaction,
                CommandTimeout = (int)timeout.TotalSeconds,
            };
        }

        public void Dispose()
        {
            Command.Dispose();
            if (_transaction == null)
                _connection.Dispose();
        }
    }
}