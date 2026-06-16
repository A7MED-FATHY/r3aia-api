using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	public class VolunteerRequest
	{
		public int Id { get; set; }

		public int PatientId { get; set; }
		[ForeignKey("PatientId")]
		public virtual Patient Patient { get; set; } = null!;

		public int? VolunteerId { get; set; }
		[ForeignKey("VolunteerId")]
		public virtual Volunteer? Volunteer { get; set; }

		public VolunteerRequestType Type { get; set; }
        public string Description { get; set; } = string.Empty;

		public decimal? Amount { get; set; }
		public PaymentMethod? PaymentMethod { get; set; }

        public string? ReceiptUrl { get; set; }

		public RequestStatus Status { get; set; } = RequestStatus.Pending;

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}
