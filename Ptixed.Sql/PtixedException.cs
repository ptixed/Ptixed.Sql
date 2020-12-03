using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ptixed.Sql
{
    internal class PtixedException : Exception
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

        public static Exception NoPrimaryKey(Type type) 
            => new PtixedException($"{type} has no primary key", type);

        public static Exception ColumnNotFound(string column)
            => new PtixedException($"Column '{column}' was not found");

        public static Exception InvalidTransacionState(string state)
            => new PtixedException($"Transaction in state {state}");

        public static Exception InvalidColumnName(string name)
            => new PtixedException($"Column named {name} was not found");

        public static Exception MissingImplementation(Type type, string method)
            => new PtixedException($"{type.Name} shoud implement {method}");

        public static Exception InvalidMapping()
            => new PtixedException($"Invalid mapping, not all columns were consumed");
    }
}
