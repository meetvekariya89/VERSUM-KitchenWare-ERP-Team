
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VERSUM.KitchenWareERP.Models
{
    public class StockMovement
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string ItemType { get; set; } = string.Empty;

        public int ItemId { get; set; }

        [Required]
        [StringLength(10)]
        public string MovementType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal PreviousStock { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal NewStock { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
