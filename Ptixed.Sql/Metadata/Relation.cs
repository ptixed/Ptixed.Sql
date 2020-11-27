using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Ptixed.Sql.Implementation;

namespace Ptixed.Sql.Metadata
{
    public class Relation
    {
        private readonly PropertyInfo _member;
        private readonly Action<object, List<object>> _setter;
        
        public readonly Type SlotType;
        public readonly bool IsCollection;

        private Relation(PropertyInfo member)
        {
            _member = member;
            
            var undertype = member.PropertyType.GetInterfaces()
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
                SlotType = member.PropertyType;
                IsCollection = false;

                var accessor = TypeAccessor.Get(member.DeclaringType);
                _setter = (self, value) => accessor[self, member] = value.SingleOrDefault();
            }
        }

        public static Relation TryCreate(PropertyInfo member)
        {
            var attrs = member.GetCustomAttributes();

            var relation = attrs.OfType<RelationAttribute>().FirstOrDefault();

            if (relation == null)
                return null;

            return new Relation(member);
        }

        public void SetValue(object self, List<object> value) => _setter(self, value);
        
        private static Action<object, List<object>> CreateSetter(PropertyInfo property, Type undertype)
        {
            var self = OpCodes.Ldarg_0;
            var value = OpCodes.Ldarg_1;

            var setter = new DynamicMethod(property.DeclaringType.FullName + "_" + property.Name + "_set", null, new[]
                {
                    typeof(object),
                    typeof(List<object>)
                }, property.DeclaringType, true);
            var il = setter.GetILGenerator();
                
            il.Emit(self);
            il.Emit(value);
            il.Emit(OpCodes.Call, typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(undertype));
            il.Emit(OpCodes.Newobj, property.PropertyType.GetConstructor(new [] { typeof(IEnumerable<>).MakeGenericType(undertype) }));
            il.Emit(OpCodes.Callvirt, property.SetMethod);
            il.Emit(OpCodes.Ret);

            return (Action<object, List<object>>)setter.CreateDelegate(typeof(Action<object, List<object>>));
        }
    }
}
