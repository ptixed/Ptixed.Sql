using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ptixed.Sql.Implementation.Accessors
{
    public class TypeAccessor
    {
        private static readonly ConcurrentDictionary<Type, TypeAccessor> Cache = new ConcurrentDictionary<Type, TypeAccessor>();

        private readonly Accessor<MemberInfo> _accessor;

        public object this[object target, MemberInfo member]
        {
            get => _accessor[target, member];
            set => _accessor[target, member] = value;
        }

        public static TypeAccessor Get(Type type) => Cache.GetOrAdd(type, t => new TypeAccessor(t));

        private TypeAccessor(Type type)
        {
            IEnumerable<MemberInfo> properties = type.GetProperties();
            IEnumerable<MemberInfo> fields = type.GetFields();

            _accessor = new Accessor<MemberInfo>(type, properties.Concat(fields).ToDictionary(x => x));
        }
    }
}
