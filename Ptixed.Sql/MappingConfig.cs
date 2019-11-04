using System;
using System.Collections.Concurrent;

namespace Ptixed.Sql
{
    public class MappingConfig
    {
        private readonly ConcurrentDictionary<Type, Func<object, object>> _todb = new ConcurrentDictionary<Type, Func<object, object>>();
        private readonly ConcurrentDictionary<Type, Func<object, object>> _fromdb = new ConcurrentDictionary<Type, Func<object, object>>();

        public object ToDb(Type type, object obj)
        {
            var ret = type == null ? obj : _todb.GetOrAdd(type, ToDbImpl)(obj);
            return ret ?? DBNull.Value;
        }

        protected virtual Func<object, object> ToDbImpl(Type type)
        {
            return x => x;
        }

        public object FromDb(Type type, object obj)
        {
            return type == null ? obj : _fromdb.GetOrAdd(type, FromDbImpl)(obj);
        }

        protected virtual Func<object, object> FromDbImpl(Type type)
        {
            return x => x;
        }
    }
}
