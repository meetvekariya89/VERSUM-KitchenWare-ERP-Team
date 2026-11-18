
using System.ComponentModel.DataAnnotations;

namespace VERSUM.KitchenWareERP.Models
{
    public class Dealer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string DealerCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? ContactPerson { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(15)]
        public string? Pincode { get; set; }

        [StringLength(15)]
        public string? GSTNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
