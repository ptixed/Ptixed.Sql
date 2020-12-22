using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;

namespace Ptixed.Sql.Implementation
{
    public interface IQueryExecutor
    {
        List<T> Query<T>(Query query, params Type[] types);
        IEnumerator<T> LazyQuery<T>(Query query, params Type[] types);
        int NonQuery(IEnumerable<Query> queries);

        List<T> GetById<T>(IEnumerable<object> ids);
        void Insert<T>(IEnumerable<T> entities);
        void Update(IEnumerable<object> entities);
        void Delete(IEnumerable<(Table table, object id)> deletes);
    }
}
