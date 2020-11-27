using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Implementation
{
    internal interface ITracker
    {
        object Get(Table table, object id);
        void Set(Table table, object id, object entity);
    }
}
