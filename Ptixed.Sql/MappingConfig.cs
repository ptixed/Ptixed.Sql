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
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var subconverter = _fromdb.GetOrAdd(type.GetGenericArguments()[0], FromDbImpl);
                return x => x == null ? null : subconverter(x);
            }

            if (!type.IsEnum && typeof(IConvertible).IsAssignableFrom(type))
                return x => x == null || x.GetType() == type
                    ? x
                    : (x is IConvertible c ? c.ToType(type, null) : ((IConvertible)x.ToString()).ToType(type, null));

            return x => x;
        }
    }
}
