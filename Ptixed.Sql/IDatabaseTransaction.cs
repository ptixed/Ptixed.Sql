using System;

namespace Ptixed.Sql
{
    public interface IDatabaseTransaction : IQueryExecutor, IDisposable
    {
        void Commit();
    }
}
