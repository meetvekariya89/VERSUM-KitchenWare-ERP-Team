
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VERSUM.KitchenWareERP.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int DealerId { get; set; }

        public Dealer? Dealer { get; set; }

        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<SaleItem> Items { get; set; } = new();
    }
}
