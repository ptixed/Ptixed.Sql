using System;
using System.Collections.Generic;

namespace Ptixed.Sql
{
    public interface IQueryExecutor
    {
        IEnumerable<T> Query<T>(Query query, params Type[] types);
        int NonQuery(params Query[] query);

        List<T> Insert<T>(params T[] entities);
        void Delete(params object[] entities);
    }
}
