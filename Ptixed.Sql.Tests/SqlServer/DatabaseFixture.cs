﻿using System;
using Ptixed.Sql.Impl;
using Ptixed.Sql.SqlServer;

namespace Ptixed.Sql.Tests.SqlServer
{
    public class DatabaseFixture : IDisposable
    {
        private readonly ConnectionConfig _config = new ConnectionConfig(AppSettings.Instance["SqlServer.ConnectionString"]);

        public DatabaseFixture()
        {
            using (var db = OpenConnection())
            {
                var drop1 = new Query($@"if exists (select * from sys.tables where name = 'Model') drop table Model");
                var create1 = new Query($@"create table Model
                    ( client int not null identity(1,1) 
	                , question uniqueidentifier not null
	                , EnumAsInt int not null
                    , EnumAsString varchar(50) null
                    , sub varchar(1000) null
                    , SomeConstant varchar(50) null
                    , [DROP] varchar(50) null
                    , created datetime
                    , primary key (client, question)
	                )");

                var drop2 = new Query($@"if exists (select * from sys.tables where name = 'Model2') drop table Model2");
                var create2 = new Query($@"create table Model2
                    ( Id int not null identity(1,1) 
	                , ModelId int null
                    , primary key (Id)
                    )");

                var drop3 = new Query($@"if exists (select * from sys.tables where name = 'ModelUpsert') drop table ModelUpsert");
                var create3 = new Query($@"create table ModelUpsert
                    ( Id int not null identity(1,1) 
	                , Name varchar(256) null
                    , Age int null
                    , primary key (Id)
                    )");

                db.NonQuery(drop1, create1, drop2, create2, drop3, create3);
            }
        }

        public void Dispose()
        {
            using (var db = OpenConnection())
                db.NonQuery(new Query($"drop table Model; drop table Model2; drop table ModelUpsert;"));
        }

        public Database OpenConnection() => new Database(_config);
    }
}
