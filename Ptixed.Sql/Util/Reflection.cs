using Ptixed.Sql.Impl;
using System.Linq.Expressions;
using System.Reflection;

namespace Ptixed.Sql.Util
{
    internal static class Reflection
    {
        public static object GetValue(MemberInfo member, object owner)
        {
            var accessor = TypeAccessor.Get(member.DeclaringType);
            switch (member)
            {
                case FieldInfo fi:
                    return accessor[owner, fi];
                case PropertyInfo pi:
                    return accessor[owner, pi];
                default:
                    throw PtixedException.InvalidExpression(member);
            }
        }

        public static object Execute(Expression expr)
        {
            switch (expr)
            {
                case ConstantExpression ce:
                    return ce.Value;
                case MemberExpression me:
                    var owner = Execute(me.Expression);
                    return GetValue(me.Member, owner);
                default:
                    throw PtixedException.InvalidExpression(expr);
            }
        }
    }
}
