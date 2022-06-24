using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Tests.Specimens;
using Xunit;

namespace Ptixed.Sql.Tests
{
    public class QueryTests : IClassFixture<DatabaseFixture>, IDisposable
    {
        private DatabaseFixture _db;

        public QueryTests(DatabaseFixture dbfixture)
        {
            _db = dbfixture;
        }

        public void Dispose()
        {
            using (var db = _db.OpenConnection())
                db.NonQuery($"DELETE FROM Model");
        }

        [Fact]
        public void TestQueries()
        {
            using (var db = _db.OpenConnection())
            {
                var model = new Model
                {
                    Id = new ModelKey()
                    {
                        QuestionId = Guid.NewGuid()
                    },
                    EnumAsString = SomeEnum.SomeEnumValue2,
                    SubModel = new SubModelClass
                    {
                        Id = 8
                    }
                };

                var inserted = db.Insert(model);
                Assert.Equal(model, inserted);
                Assert.NotEqual(0, model.Id.ClientId);

                var selected = db.GetById<Model>(model.Id);
                Assert.Equal(model.Id.ClientId, selected.Id.ClientId);
                Assert.Equal(model.Id.QuestionId, selected.Id.QuestionId);
                Assert.Equal(model.CreatedAt, selected.CreatedAt);
                Assert.Equal(model.EnumAsInt, selected.EnumAsInt);
                Assert.Equal(model.EnumAsString, selected.EnumAsString);
                Assert.Equal(model.SubModel.Id, selected.SubModel.Id);

                model.CreatedAt = new DateTime(2019, 10, 19, 17, 21, 0);
                db.Update(model);
                selected = db.GetById<Model>(model.Id);
                Assert.Equal(model.CreatedAt, selected.CreatedAt);

                db.Delete(model);
                Assert.Null(db.GetById<Model>(model.Id));
            }
        }

        [Fact]
        public void TestRelations()
        {
            using (var db = _db.OpenConnection())
            {
                var model = db.Insert(new Model
                {
                    Id = new ModelKey
                    {
                        QuestionId = Guid.NewGuid()
                    }
                });
                db.Insert(new Model2
                {
                    ModelId = model.Id.ClientId
                });
                db.Insert(new Model2
                {
                    ModelId = model.Id.ClientId
                });

                var q1 = new Query($@"SELECT m.*, m2.* FROM Model m JOIN Model2 m2 ON m2.ModelId = m.client WHERE m.client = {model.Id.ClientId}");
                var read1 = db.Query<Model>(q1, typeof(Model), typeof(Model2)).Single();
                Assert.Equal(2, read1.Related2.Count);
                Assert.Equal(read1, read1.Related2[0].Model);
                Assert.Equal(read1, read1.Related2[1].Model);

                var q2 = new Query($@"SELECT m.*, m2.* FROM Model m LEFT JOIN Model2 m2 ON m2.ModelId = -1 WHERE m.client = {model.Id.ClientId}");
                var read2 = db.Query<Model>(q2, typeof(Model), typeof(Model2)).Single();
                Assert.Empty(read2.Related2);
            }
        }

        [Fact]
        public void TestTransactions()
        {
            using (var db = _db.OpenConnection())
            {
                var model = new Model
                {
                    Id = new ModelKey()
                    {
                        QuestionId = Guid.NewGuid()
                    }
                };

                using (var tran = db.OpenTransaction(IsolationLevel.Serializable))
                {
                    db.Insert(model);
                }
                Assert.Null(db.GetById<Model>(model.Id));

                using (var tran = db.OpenTransaction(IsolationLevel.Serializable))
                {
                    db.Insert(model);
                    tran.Commit();
                }
                Assert.NotNull(db.GetById<Model>(model.Id));
            }
        }

        [Fact]
        public void TestScalarQueries()
        {
            using (var db = _db.OpenConnection())
            {
                var model = new Model
                {
                    Id = new ModelKey()
                    {
                        QuestionId = Guid.NewGuid()
                    }
                };
                db.Insert(model);

                var count = db.Single<int>($"SELECT COUNT(*) FROM Model");
                Assert.Equal(1, count);

                var countd = db.Single<decimal?>($"SELECT COUNT(*) FROM Model");
                Assert.Equal(1, countd);

                var now = db.Single<DateTime?>($"SELECT CURRENT_TIMESTAMP");
                Assert.Equal(DateTime.Now.Date, now.Value.Date);

                var str = db.Single<string>($"SELECT SomeConstant FROM Model");
                Assert.Equal(model.SomeConstant, str);
            }
        }

        [Fact]
        public void TestModelWithNoPk()
        {
            using (var db = _db.OpenConnection())
            {
                var model = new ModelWithNoPk
                {
                    QuestionId = Guid.NewGuid(),
                    EnumAsString = SomeEnum.SomeEnumValue2,
                    SubModel = new SubModelClass
                    {
                        Id = 8
                    }
                };

                var inserted = db.Insert(model);

                var selected = db.Single<Model>($"SELECT * FROM Model");
                Assert.Equal(model.QuestionId, selected.Id.QuestionId);
                Assert.Equal(model.CreatedAt, selected.CreatedAt);
                Assert.Equal(model.EnumAsInt, selected.EnumAsInt);
                Assert.Equal(model.EnumAsString, selected.EnumAsString);
                Assert.Equal(model.SubModel.Id, selected.SubModel.Id);

                var selected2 = db.Single<ModelWithNoPk>($"SELECT * FROM Model");
                Assert.Equal(model.QuestionId, selected2.QuestionId);
                Assert.Equal(model.CreatedAt, selected2.CreatedAt);
                Assert.Equal(model.EnumAsInt, selected2.EnumAsInt);
                Assert.Equal(model.EnumAsString, selected2.EnumAsString);
                Assert.Equal(model.SubModel.Id, selected2.SubModel.Id);
            }
        }

        [Fact]
        public void TestDictionaryResult()
        {
            using (var db = _db.OpenConnection())
            {
                var model = new Model
                {
                    Id = new ModelKey()
                    {
                        QuestionId = Guid.NewGuid()
                    },
                    SubModel = new SubModelClass
                    {
                        Id = 777
                    }
                };
                db.Insert(model);

                var read1 = db.Single<IDictionary<string, object>>($"SELECT * FROM Model");
                Assert.Equal(model.Id.ClientId, read1["client"]);
            }
        }

        [Fact]
        public void TestTuples()
        {
            using (var db = _db.OpenConnection())
            {
                var model = db.Insert(new Model
                {
                    Id = new ModelKey
                    {
                        QuestionId = Guid.NewGuid()
                    }
                });
                db.Insert(new Model2
                {
                    ModelId = model.Id.ClientId
                });
                db.Insert(new Model2
                {
                    ModelId = model.Id.ClientId
                });

                var (selected, count) = db.Single<(Model, int)>($"SELECT *, (SELECT COUNT(*) FROM Model2 WHERE Model2.ModelId = Model.client) c FROM Model");

                Assert.Equal(model.Id.ClientId, selected.Id.ClientId);
                Assert.Equal(model.Id.QuestionId, selected.Id.QuestionId);

                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void TestAffectedRows()
        {
            using (var db = _db.OpenConnection())
            {
                db.Insert(new Model2
                {
                    ModelId = 1
                });
                db.Insert(new Model2
                {
                    ModelId = 2
                });

                var q1 = new Query($"DELETE FROM Model2 WHERE Id = 1");
                var q2 = new Query($"DELETE FROM Model2 WHERE Id = 2");

                var affected = db.NonQuery(q1, q2);

                Assert.Equal(2, affected);
            }
        }

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
            members.Add(".ctor`1", typeof(Foo).GetConstructor(new [] { typeof(int), typeof(Foo) }));
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
        }
    }
}
