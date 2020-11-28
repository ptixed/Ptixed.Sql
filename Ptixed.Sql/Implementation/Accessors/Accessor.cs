using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Ptixed.Sql.Implementation.Accessors
{
    public class Accessor<TKey>
    {
        private readonly Func<object, int, object> _getter;
        private readonly Action<object, int, object> _setter;
        private readonly Func<object> _createnew;
        
        public readonly Type Type;
        public readonly Dictionary<TKey, int> Lookup;

        public bool CreateNewSupported => _createnew != null;

        public object this[object target, TKey name]
        {
            get => _getter(target, Lookup[name]);
            set => _setter(target, Lookup[name], value);
        }

        public Accessor(Type type, Dictionary<TKey, MemberInfo> lookup)
        {
            Type = type;

            Lookup = lookup.Select((x, i) => (Key: x.Key, Index: i)).ToDictionary(x => x.Key, x => x.Index);

            var members = lookup.Select(x => x.Value).ToArray();

            _setter = CreateSetter(type, members);
            _getter = CreateGetter(type, members);
            _createnew = CreateCreateNew(type);
        }

        public object CreateNew() => _createnew();

        private static Func<object, int, object> CreateGetter(Type type, MemberInfo[] members)
        {
            var self = OpCodes.Ldarg_0;
            var index = OpCodes.Ldarg_1;

            var getter = new DynamicMethod(type.FullName + "_get", typeof(object), new[]
                {
                    typeof(object),
                    typeof(int),
                }, type.Module, true);
            var il = getter.GetILGenerator();

            var labels = members.Select(x => il.DefineLabel()).ToArray();

            il.Emit(self);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox, type);

            il.Emit(index);
            il.Emit(OpCodes.Switch, labels);

            for (int i = 0; i < members.Length; ++i)
            {
                il.MarkLabel(labels[i]);
                switch (members[i])
                {
                    case PropertyInfo pi:
                        il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, pi.GetMethod);
                        if (pi.PropertyType.IsValueType)
                            il.Emit(OpCodes.Box, pi.PropertyType);
                        break;
                    case FieldInfo fi:
                        il.Emit(OpCodes.Ldfld, fi);
                        if (fi.FieldType.IsValueType)
                            il.Emit(OpCodes.Box, fi.FieldType);
                        break;
                    default:
                        throw PtixedException.InvalidExpression(members[i]);
                }
                il.Emit(OpCodes.Ret);
            }

            return (Func<object, int, object>)getter.CreateDelegate(typeof(Func<object, int, object>));
        }

        private static Action<object, int, object> CreateSetter(Type type, MemberInfo[] members)
        {
            var self = OpCodes.Ldarg_0;
            var index = OpCodes.Ldarg_1;
            var value = OpCodes.Ldarg_2;

            var setter = new DynamicMethod(type.FullName + "_set", null, new[]
                {
                    typeof(object),
                    typeof(int),
                    typeof(object),
                }, type.Module, true);
            var il = setter.GetILGenerator();

            var labels = members.Select(x => il.DefineLabel()).ToArray();

            il.Emit(self);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox, type);

            il.Emit(index);
            il.Emit(OpCodes.Switch, labels);

            for (int i = 0; i < members.Length; ++i)
            {
                il.MarkLabel(labels[i]);

                switch (members[i])
                {
                    case PropertyInfo pi when pi.CanWrite:
                        il.Emit(value);
                        il.Emit(pi.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, pi.PropertyType);
                        il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, pi.SetMethod);
                        break;
                    case PropertyInfo pi:
                        il.Emit(OpCodes.Pop);
                        break;
                    case FieldInfo fi:
                        il.Emit(value);
                        il.Emit(fi.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, fi.FieldType);
                        il.Emit(OpCodes.Stfld, fi);
                        break;
                    default:
                        throw PtixedException.InvalidExpression(members[i]);
                }

                il.Emit(OpCodes.Ret);
            }

            return (Action<object, int, object>)setter.CreateDelegate(typeof(Action<object, int, object>));
        }

        private static Func<object> CreateCreateNew(Type type)
        {
            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (ctor == null)
                return null;

            var createnew = new DynamicMethod(type.FullName + "_new", typeof(object), Array.Empty<Type>(), type.Module, true);
            var il = createnew.GetILGenerator();

            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            return (Func<object>)createnew.CreateDelegate(typeof(Func<object>));
        }
    }
}
