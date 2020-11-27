using System;
using System.Diagnostics.CodeAnalysis;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Tests.Specimens
{
    public class SubModelClass
    {
        public int Id { get; set; }
    }

    public enum SomeEnum
    {
        SomeEnumValue1,
        SomeEnumValue2
    }

    public class ModelKey
    {
        [Column("client", IsAutoIncrement = true)]
        public int ClientId { get; set; }
        [Column("question")]
        public Guid QuestionId { get; set; }

        public override bool Equals(object obj) => obj is ModelKey other && other.ClientId == ClientId && QuestionId == other.QuestionId;
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => ClientId ^ QuestionId.GetHashCode();
    }
}
