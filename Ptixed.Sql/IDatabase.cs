using Ptixed.Sql.Implementation;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Ptixed.Sql
{
    public interface IDatabase : IQueryExecutor, IDisposable
    {
        DatabaseConfig Config { get; }
        DatabaseDiagnostics Diagnostics { get; }

        IDatabaseTransaction OpenTransaction(IsolationLevel? isolation = null);
        SqlCommand CreateCommand();
        void ResetConnection();
    }
}
