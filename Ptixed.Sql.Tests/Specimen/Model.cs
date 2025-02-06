using System;
using System.Collections.Generic;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Meta;

namespace Ptixed.Sql.Tests.Specimen
{
    [Table("Model", nameof(Id))]
    internal class Model
    {
        [Column]
        [SqlConverter(typeof(CompositeColumnConverter))]
        public ModelKey Id { get; set; }

        [Column]
        public SomeEnum EnumAsInt { get; set; }
        
        [SqlConverter(typeof(EnumToStringConverter))]
        public SomeEnum EnumAsString { get; set; }

        [Ignore]
        public string Temp { get; set; }

        [SqlConverter(typeof(JsonSqlConverter), "sub")]
        public SubModelClass SubModel { get; set; }

        public string SomeConstant = "SomeConstantValue";
        public string DROP => "DROP";

        [Column("created")]
        public DateTime CreatedAt { get; set; } = new DateTime(2019, 10, 19, 13, 9, 0);

        [Relation]
        public List<Model2> Related2 { get; set; }
    }
}
