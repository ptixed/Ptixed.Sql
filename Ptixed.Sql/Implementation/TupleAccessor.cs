using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Ptixed.Sql.Implementation
{
    public class TupleAccessor
    {
        private static readonly ConcurrentDictionary<Type, TupleAccessor> Cache = new ConcurrentDictionary<Type, TupleAccessor>();
        private static readonly HashSet<Type> TupleTypes = new HashSet<Type>
        {
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>),
            typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>),
            typeof(Tuple<,,,,,,,>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        };

        private readonly Func<object[], object> _createnew;

        public readonly Type Type;
        public readonly Type[] Types;

        public static TupleAccessor Get(Type type) => Cache.GetOrAdd(type, t =>
        {
            if (!t.IsGenericType || !TupleTypes.Contains(t.GetGenericTypeDefinition()))
                return null;
            return new TupleAccessor(t);
        });

        private TupleAccessor(Type type)
        {
            Type = type;
            Types = type.GetGenericArguments();
            _createnew = CreateCreateNew(Type, Types);
        }

        public object CreateNew(object[] args) => _createnew(args);

        private static Func<object[], object> CreateCreateNew(Type type, Type[] types)
        {
            var ctor = type.GetConstructor(types);
            if (ctor == null)
                return null;

            var values = OpCodes.Ldarg_0;

            var createnew = new DynamicMethod(type.FullName + "_new", typeof(object), new[] { typeof(object[]) }, true);
            var il = createnew.GetILGenerator();

            for (int i = 0; i < types.Length; ++i)
            {
                il.Emit(values);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(types[i].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, types[i]);
            }

            il.Emit(OpCodes.Newobj, ctor);
            if (type.IsValueType)
                il.Emit(OpCodes.Box, type);
            il.Emit(OpCodes.Ret);

            return (Func<object[], object>)createnew.CreateDelegate(typeof(Func<object[], object>));
        }
    }
}
