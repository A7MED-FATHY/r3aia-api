using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
    public class PatientCase
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        public int? GovernorateId { get; set; }
        public Governorate? Governorate { get; set; }

        public CaseType CaseType { get; set; } = CaseType.Treatment;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RequiredAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CollectedAmount { get; set; } = 0;

        public CaseStatus Status { get; set; } = CaseStatus.Pending;

        /// <summary>
        /// Stored as comma-separated relative paths
        /// </summary>
        public string? ImagesJson { get; set; }

        [NotMapped]
        public List<string> Images
        {
            get => string.IsNullOrEmpty(ImagesJson)
                ? new List<string>()
                : ImagesJson.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
            set => ImagesJson = value != null ? string.Join('|', value) : null;
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        // Navigation
        public virtual ICollection<PatientDonation> Donations { get; set; } = new List<PatientDonation>();
    }
}
