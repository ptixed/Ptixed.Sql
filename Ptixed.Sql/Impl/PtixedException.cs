using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Ptixed.Sql.Impl
{
    public class PtixedException : Exception
    {
        public readonly object Context;
        
        private PtixedException(string message, object context = null)
            : base(message)
        {
            Context = context;
        }
        
        public static Exception InvalidExpression(Expression expresssion) 
            => new PtixedException("Query was not in valid format", expresssion);

        public static Exception InvalidExpression(MemberInfo member) 
            => new PtixedException("Query was not in valid format", member);

        public static Exception ColumnNotFound(string column, IEnumerable<string> dataset)
        {
            var sb = new StringBuilder();
            sb.Append($"Column '{column}' was not found in result set. ");
            sb.Append($"Columns available in result set where: {string.Join(", ", dataset)}. ");
            sb.Append($"Verify this set corresponds with how classes you map to are defined in code");
            return new PtixedException(sb.ToString());
        }

        public static Exception InvalidTransacionState(string state)
            => new PtixedException($"Transaction in state {state}");

        public static Exception ColumnNotFound(string name, Type type)
            => new PtixedException($"Column named {name} was not found on type {type.AssemblyQualifiedName}");

        public static Exception MissingImplementation(Type type, string method)
            => new PtixedException($"{type.Name} shoud implement {method}");

        public static Exception InvalidMapping()
            => new PtixedException($"Invalid mapping, not all columns were consumed");
    }
}
