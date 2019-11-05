using Ptixed.Sql.Meta;

namespace Ptixed.Sql.Tests.Specimens
{
    [Table("Model2", nameof(Id))]
    internal class Model2
    {
        [Column(IsAutoIncrement = true)]
        public int Id { get; set; }

        [Column]
        public int ModelId { get; set; }

        [Relation]
        public Model Model { get; set; }
    }
}
