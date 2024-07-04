using System;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Postgres;

namespace Ptixed.Sql.Tests.Postgres
{
    public class DatabaseFixture : IDisposable
    {
        private readonly ConnectionConfig _config = new ConnectionConfig(AppSettings.Instance["Postgres.ConnectionString"]);

        public DatabaseFixture()
        {
            using (var db = OpenConnection())
            {
                var drop1 = new Query($@"drop table if exists ""Model""");
                var create1 = new Query($@"create table ""Model""
                    ( ""client"" serial not null
	                , ""question"" uuid not null
	                , ""EnumAsInt"" int not null
                    , ""EnumAsString"" varchar(50) null
                    , ""sub"" varchar(1000) null
                    , ""SomeConstant"" varchar(50) null
                    , ""DROP"" varchar(50) null
                    , ""created"" timestamp
                    , primary key (""client"", ""question"")
	                )");

                var drop2 = new Query($@"drop table if exists ""Model2""");
                var create2 = new Query($@"create table ""Model2""
                    ( ""Id"" serial not null
	                , ""ModelId"" int null
                    , primary key (""Id"")
                    )");

                var drop3 = new Query($@"drop table if exists ""ModelUpsert""");
                var create3 = new Query($@"create table ""ModelUpsert""
                    ( ""Id"" serial not null
	                , ""Name"" varchar(256) null
                    , ""Age"" int null
                    , primary key (""Id"")
                    )");

                db.NonQuery(drop1, create1, drop2, create2, drop3, create3);
            }
        }

        public void Dispose()
        {
            using (var db = OpenConnection())
                db.NonQuery(new Query($@"drop table ""Model""; drop table ""Model2""; drop table ""ModelUpsert"";"));
        }

        public Database OpenConnection() => new Database(_config);
    }
}
