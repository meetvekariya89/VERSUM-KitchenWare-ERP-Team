
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VERSUM.KitchenWareERP.Models
{
    public class ProductionBatch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string BatchNumber { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public int RawMaterialId { get; set; }

        public int QuantityProduced { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MaterialConsumed { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime ProductionDate { get; set; }
            = DateTime.UtcNow;
    }
}
