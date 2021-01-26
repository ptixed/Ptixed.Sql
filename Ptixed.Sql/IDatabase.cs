using Ptixed.Sql.Implementation;
using System;
using System.Data;

namespace Ptixed.Sql
{
    public interface IDatabase : IDatabaseCore, IQueryExecutor, IDisposable
    {
        DatabaseDiagnostics Diagnostics { get; }
        ITransactionContext OpenTransaction(IsolationLevel? isolation = null, bool tracking = false);
        void ResetConnection();
    }
}
