using System.Data;
using System.Data.Common;

namespace Ptixed.Sql
{
    public interface IDatabase<TParameter> : IDatabaseAccessor<TParameter>
        where TParameter : DbParameter, new()
    {
        MappingConfig MappingConfig { get; }

        IDatabaseTransaction<TParameter> OpenTransaction(IsolationLevel isolation);
    }
}
