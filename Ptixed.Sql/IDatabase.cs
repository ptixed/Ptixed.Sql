using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Ptixed.Sql
{
    public interface IDatabase<TParameter>
        where TParameter : DbParameter, new()
    {
        MappingConfig MappingConfig { get; }

        IEnumerable<T> Query<T>(Query<TParameter> query, params Type[] types);
        int NonQuery(params Query<TParameter>[] query);

        IDatabaseTransaction OpenTransaction(IsolationLevel isolation);
    }
}
