using System;
using System.Data.SqlClient;
using Ptixed.Sql.Implementation;

namespace Ptixed.Sql
{
    public interface ITransactionContext : IQueryExecutor, IDisposable
    {
        bool IsDisposed { get; }
        bool? IsCommited { get; }

        SqlTransaction SqlTransaction { get; }

        void Commit();
    }
}
