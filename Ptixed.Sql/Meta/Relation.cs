using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Util;

namespace Ptixed.Sql.Meta
{
    public class Relation
    {
        private readonly MemberInfo _member;
        private readonly Action<object, List<object>> _setter;
        
        public readonly Type SlotType;
        public readonly bool IsCollection;

        private Relation(MemberInfo member)
        {
            _member = member;
            
            var undertype = member.GetMemberType().GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()
                .FirstOrDefault();
            
            if (undertype != null)
            {
                SlotType = undertype;
                IsCollection = true;

                _setter = CreateSetter(member, undertype);
            }
            else
            {
                SlotType = member.GetMemberType();
                IsCollection = false;

                var accessor = TypeAccessor.Get(member.DeclaringType);
                _setter = (self, value) => accessor[self, member] = value.SingleOrDefault();
            }
        }

        public static Relation TryCreate(MemberInfo member)
        {
            var attrs = member.GetCustomAttributes();

            var relation = attrs.OfType<RelationAttribute>().FirstOrDefault();

            if (relation == null)
                return null;

            return new Relation(member);
        }

        public void SetValue(object self, List<object> value) => _setter(self, value);
        
        private static Action<object, List<object>> CreateSetter(MemberInfo member, Type undertype)
        {
            var self = OpCodes.Ldarg_0;
            var value = OpCodes.Ldarg_1;

            var setter = new DynamicMethod(member.DeclaringType.FullName + "_" + member.Name + "_set", null, new[]
                {
                    typeof(object),
                    typeof(List<object>)
                }, member.DeclaringType, true);
            var il = setter.GetILGenerator();
                
            il.Emit(self);
            il.Emit(value);
            il.Emit(OpCodes.Call, typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(undertype));
            il.Emit(OpCodes.Newobj, member.GetMemberType().GetConstructor(new [] { typeof(IEnumerable<>).MakeGenericType(undertype) }));
            switch (member)
            {
                case FieldInfo fi:
                    il.Emit(OpCodes.Stfld, fi);
                    break;
                case PropertyInfo pi:
                    il.Emit(OpCodes.Callvirt, pi.SetMethod);
                    break;
                default:
                    throw PtixedException.InvalidExpression(member);
            }
            il.Emit(OpCodes.Ret);

            return (Action<object, List<object>>)setter.CreateDelegate(typeof(Action<object, List<object>>));
        }
    }
}
