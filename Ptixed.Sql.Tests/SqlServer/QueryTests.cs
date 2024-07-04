using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Ptixed.Sql.SqlServer;
using Ptixed.Sql.Tests.Specimen;
using Xunit;

namespace Ptixed.Sql.Tests.SqlServer
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
            {
                db.NonQuery($"DELETE FROM Model");
                db.NonQuery($"DELETE FROM Model2");
            }
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


        [Fact]
        public void TestListInsert()
        {
            using (var db = _db.OpenConnection())
                Assert.Throws<SqlException>(() =>
                    db.Insert(new List<Model2>
                    { 
                        new Model2
                        {
                            ModelId = 1
                        }
                    })
                );
        }

        [Fact]
        public void TestDictionaryInsert()
        {
            using (var db = _db.OpenConnection())
            {
                var autoincr = db.Insert("Model", new Dictionary<string, object>
                {
                    { "question", Guid.NewGuid() },
                    { "EnumAsInt", (int)SomeEnum.SomeEnumValue1 },
                    { "EnumAsString", SomeEnum.SomeEnumValue2.ToString() },
                    { "sub", "{ id: 1 }" },
                    { "SomeConstant", "SomeConstantValue" },
                    { "created", DateTime.Now }
                });
                var model = db.Single<Model>($"SELECT * FROM Model");
                Assert.Equal(autoincr, model.Id.ClientId.ToString());
            }
        }

        [Fact]
        public void TestUpsertQueries()
        {
            using (var db = _db.OpenConnection())
            {
                var model = new ModelUpsert
                {
                    Age = 1,
                    Name = "John Doe"
                };

                var inserted = db.Insert(model);
                Assert.True(inserted.Id > 0);

                var response = db.Upsert($"ON [Target].Id = [Source].Id", new ModelUpsert
                {
                    Name = "Jane Doe",
                    Age = 12,
                    Id = inserted.Id
                });
                Assert.NotNull(response);
                Assert.True(response.Age == 12);

                var byId = db.GetById<ModelUpsert>(inserted.Id);
                Assert.NotNull(byId);
                Assert.True(byId.Age == 12);

                response = db.Upsert($"ON [Target].Id = [Source].Id", new ModelUpsert
                {
                    Name = "Janice Doe",
                    Age = 21
                });
                Assert.NotNull(response);
                Assert.True(response.Age == 21);
                Assert.True(response.Id != inserted.Id);
                var byName = db.ToList<ModelUpsert>($"SELECT * FROM ModelUpsert WHERE Name like 'Janice Doe'");
                Assert.NotNull(byName);
                Assert.True(byName.First().Age == 21);
                Assert.True(byName.First().Id != inserted.Id);
            }
        }

        [Fact]
        public void FormattableStringWithinFormattableString()
        {
            using (var db = _db.OpenConnection())
            {
                var model = new ModelUpsert
                {
                    Age = 1,
                    Name = "John Doe"
                };

                var inserted = db.Insert(model);
                Assert.True(inserted.Id > 0);

                FormattableString q1 = $"{model.Id}";
                FormattableString q2 = $"Id = {q1}";
                Query q3 = new Query($"WHERE {q2} or Id = {q1}");
                Query q4 = new Query($"SELECT * FROM ModelUpsert {q3}");

                var result = db.Query<ModelUpsert>(q4).Single();
                Assert.Equal(result.Id, model.Id);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public void TestBulkInsert(int n)
        {
            using (var db = _db.OpenConnection())
            {
                var data = new List<Model2>();
                for (var i = 0; i < n; ++i)
                    data.Add(new Model2
                    {
                        ModelId = i
                    });

                db.BulkInsert(data);

                var inserted = db.Single<int>($"SELECT COUNT(*) FROM Model2");
                Assert.Equal(n, inserted);
            }
        }
    }
}
