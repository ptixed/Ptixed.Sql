using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;

namespace Ptixed.Sql.Implementation
{
    internal interface IQueryExecutor
    {
        QueryResult<T> Query<T>(Query query, IEnumerable<Type> types);
        int NonQuery(IEnumerable<Query> queries);

        List<T> Insert<T>(params T[] entities);
        void Update(IEnumerable<object> entities);
        void Delete(IEnumerable<(Table table, object id)> keys);
    }
}
