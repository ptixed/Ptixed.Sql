using Ptixed.Sql.Metadata;
using System.Collections.Generic;

namespace Ptixed.Sql.Implementation.Trackers
{
    internal class DefaultTracker : ITracker
    {
        private readonly Dictionary<(Table, object), object> _store = new Dictionary<(Table, object), object>();

        public object Get(Table table, object id) => _store.TryGetValue((table, id), out object ret) ? ret : null;
        public void Set(Table table, object id, object entity) => _store[(table, id)] = entity;
    }
}
