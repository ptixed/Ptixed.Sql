using System;
using System.Data.Common;

namespace Ptixed.Sql
{
    public interface IDatabaseTransaction<TParameter> : IDatabaseAccessor<TParameter>, IDisposable
        where TParameter : DbParameter, new() 
    {
        void Commit();
    }
}
