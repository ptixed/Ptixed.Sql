using System;
using System.Collections.Generic;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Meta;

namespace Ptixed.Sql.Tests.Specimens
{
    [Table("ModelUpsert", nameof(Id))]
    internal class ModelUpsert
    {
        [Column(IsAutoIncrement = true)]
        public int Id { get; set; }

        [Column]
        public string Name { get; set; }
        [Column]
        public int Age { get; set; }
    }
}
