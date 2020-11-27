using System;

namespace Ptixed.Sql.Implementation
{
    public interface IEntityTracker
    {
        object Get(Type type, object id);
        void Store(Type type, object id);
    }
}
