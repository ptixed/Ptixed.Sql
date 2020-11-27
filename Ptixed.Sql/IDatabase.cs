using System;
using System.Data;

namespace Ptixed.Sql
{
    public interface IDatabase : IQueryExecutor, IDisposable
    {
        DatabaseConfig Config { get; }
        DatabaseDiagnostics Diagnostics { get; }

        IDatabaseTransaction OpenTransaction(IsolationLevel? isolation = null);

        void ResetConnection();
    }
}
