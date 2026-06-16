using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
    public class R3aiaBoxOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Shipped, Delivered, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    }
}
