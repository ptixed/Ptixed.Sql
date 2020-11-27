using System;
using System.Collections.Generic;

namespace Ptixed.Sql
{
    public interface IQueryExecutor
    {
        IEnumerable<T> Query<T>(Query query, params Type[] types);
        int NonQuery(params Query[] queries);

        T Insert<T>(T entity);
        List<T> Insert<T>(params T[] entities);
        void Update(params object[] entities);
        void Delete(params object[] entities);
        void Delete<T>(params object[] ids);
    }
}
