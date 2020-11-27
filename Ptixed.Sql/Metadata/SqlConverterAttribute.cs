using System;
using System.Linq;
using Ptixed.Sql.Implementation;

namespace Ptixed.Sql.Metadata
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlConverterAttribute : Attribute
    {
        private readonly Type _type;
        private readonly object[] _args;

        public SqlConverterAttribute(Type converter, params object[] ctorargs)
        {
            _type = converter;
            _args = ctorargs;
        }

        public ISqlConverter CreateConverter()
        {
            var ctor = _type.GetConstructor(_args.Select(x => x.GetType()).ToArray());
            if (ctor == null)
                throw PtixedException.MissingImplementation(_type, ".ctor");
            return (ISqlConverter)ctor.Invoke(_args);
        }
    }
}
