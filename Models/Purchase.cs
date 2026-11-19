
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VERSUM.KitchenWareERP.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string PurchaseNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? InvoiceNumber { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        public RawMaterial? RawMaterial { get; set; }

        [Range(0.001, 999999999)]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Range(0, 999999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
