using Ptixed.Sql.Metadata;
using System;
using System.Collections.Generic;

namespace Ptixed.Sql.Implementation.Trackers
{
    internal class TransactionalTracker : ITracker
    {
        private readonly List<(Table table, object id)> _deletes = new List<(Table table, object id)>();
        private readonly DefaultTracker _commited;
        private readonly DefaultTracker _uncommited;

        public object Get(Table table, object id)
        {
            return _commited.Get(table, id);
        }

        public void Set(Table table, object id, object entity)
        {
            _commited.Set(table, id, entity);
            _uncommited.Set(table, id, table.Clone(entity));
        }

        public void ScheduleDelete(Table type, object id) => _deletes.Add((type, id));

        public Query PrepareChangesQuery()
        {
            // prepare updates and deletes queries

            _deletes.Clear();

            // commit tracked objects 

            throw new NotImplementedException();
        }
    }
}
