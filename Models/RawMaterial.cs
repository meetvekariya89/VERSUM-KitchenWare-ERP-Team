
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VERSUM.KitchenWareERP.Models
{
    public class RawMaterial
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = "Kg";

        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentStock { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MinimumStock { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPerUnit { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;
    }
}
