using R3AIA.Models;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	public class Donation
	{
		public int Id { get; set; }

		public int? VolunteerId { get; set; }
		public Volunteer? Volunteer { get; set; }

		public string? DonorUserId { get; set; }

		[ForeignKey("DonationCase")]
		public int CaseId { get; set; }
		public virtual DonationCase DonationCase { get; set; } = null!;

		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }
		public string ReceiptImage { get; set; } = null!;

		public DonationStatus Status { get; set; } = DonationStatus.Pending;
		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}