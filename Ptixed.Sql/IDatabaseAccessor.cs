using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Ptixed.Sql
{
    public interface IDatabaseAccessor<TParameter>
        where TParameter : DbParameter, new()
    {
        IEnumerable<T> Query<T>(Query<TParameter> query, params Type[] types);
        int NonQuery(params Query<TParameter>[] query);
    }
}