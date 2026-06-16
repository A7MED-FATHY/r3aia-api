using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
    public class PatientDonation
    {
        public int Id { get; set; }

        [Required]
        public int PatientCaseId { get; set; }
        public virtual PatientCase PatientCase { get; set; } = null!;

        // nullable → supports guest donations
        public string? DonorId { get; set; }
        public ApplicationUser? Donor { get; set; }

        [MaxLength(150)]
        public string? DonorName { get; set; }

        [MaxLength(20)]
        public string? DonorPhone { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.InstaPay;

        /// <summary>
        /// Relative path to the proof/receipt image (optional)
        /// </summary>
        public string? ProofImage { get; set; }

        public DonationStatus Status { get; set; } = DonationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
