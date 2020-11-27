using Ptixed.Sql.Metadata;
using System.Collections.Generic;

namespace Ptixed.Sql.Implementation.Trackers
{
    internal class DefaultTracker : ITracker
    {
        public readonly Dictionary<(Table, object), object> Store = new Dictionary<(Table, object), object>();

        public object Get(Table table, object id) => Store.TryGetValue((table, id), out object ret) ? ret : null;
        public void Set(Table table, object id, object entity) => Store[(table, id)] = entity;
    }
}
