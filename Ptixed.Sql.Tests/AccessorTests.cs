using Ptixed.Sql.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Ptixed.Sql.Tests
{
    public class AccessorTests
    {
        class Foo
        {
            public static int StaticField;
            public static int StaticProperty { get; set; }

            public int Field;
            public static Foo Property { get; set; }

            private int PrivateFoo()
            {
                return 1;
            }

            private static Foo StaticFoo()
            {
                return new Foo() { Field = 1 };
            }

            public void VoidMethod(int i, Foo f)
            {
                Field = i;
            }

            public Foo Method()
            {
                return this;
            }

            public Foo()
            {

            }

            public Foo(int i, Foo f)
            {
                Field = i;
            }

            public Foo(string s)
            {

            }
        }

        [Fact]
        public void TestAccessor()
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            var members = typeof(Foo).GetMethods(flags).ToDictionary(x => x.Name, x => (MemberInfo)x);
            members.Add(".ctor", typeof(Foo).GetConstructor(Type.EmptyTypes));
            members.Add(".ctor`1", typeof(Foo).GetConstructor(new[] { typeof(int), typeof(Foo) }));
            members.Add(".ctor`1b", typeof(Foo).GetConstructor(new[] { typeof(string) }));
            foreach (var member in typeof(Foo).GetProperties(flags))
                members.Add(member.Name, member);
            foreach (var member in typeof(Foo).GetFields(flags))
                members.Add(member.Name, member);

            var accessor = new Accessor<string>(typeof(Foo), members);

            var foo = accessor.Invoke<Foo>(null, ".ctor");
            Assert.NotNull(foo);
            foo = accessor.Invoke<Foo>(null, ".ctor`1", 1, new Foo());
            Assert.Equal(1, foo.Field);
            accessor[null, "StaticField"] = 1;
            Assert.Equal(1, accessor[null, "StaticField"]);
            accessor[null, "StaticProperty"] = 1;
            Assert.Equal(1, accessor[null, "StaticProperty"]);
            accessor[foo, "Field"] = 1;
            Assert.Equal(1, accessor[foo, "Field"]);
            accessor[foo, "Property"] = foo;
            Assert.Equal(foo, accessor[foo, "Property"]);
            var v1 = accessor.Invoke<int>(foo, "PrivateFoo");
            Assert.Equal(1, v1);
            var v2 = accessor.Invoke<Foo>(null, "StaticFoo");
            Assert.Equal(1, v2.Field);
            var v3 = accessor.Invoke<object>(foo, "Method");
            Assert.Equal(foo, v3);
            var v4 = accessor.Invoke<object>(foo, "VoidMethod", 1, new Foo());
            Assert.Equal(1, foo.Field);
            Assert.Null(v4);

            var accessor2 = new Accessor<string>(typeof(KeyValuePair<string, object>), typeof(KeyValuePair<string, object>).GetProperties().ToDictionary(x => x.Name, x => x as MemberInfo));
            var v5 = new KeyValuePair<string, object>("foo", new Foo());
            Assert.Equal("foo", accessor2[v5, "Key"]);
        }
    }
}
