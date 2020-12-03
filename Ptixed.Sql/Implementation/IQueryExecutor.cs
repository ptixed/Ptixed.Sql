using Ptixed.Sql.Metadata;
using System.Collections.Generic;

namespace Ptixed.Sql.Implementation
{
    public interface IQueryExecutor
    {
        IEnumerable<T> Query<T>(Query query, params Table[] tables);
        int NonQuery(IEnumerable<Query> queries);

        List<T> GetById<T>(IEnumerable<object> ids);
        void Insert(IEnumerable<object> entities);
        void Update(IEnumerable<object> entities);
        void Delete(IEnumerable<(Table table, object id)> deletes);
    }
}
