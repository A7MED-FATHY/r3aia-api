using R3AIA.Models;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	public class MedicineRequest
	{
		public int Id { get; set; }

		public int PatientId { get; set; }
		[ForeignKey("PatientId")]
		public virtual Patient Patient { get; set; } = null!;

		public int? PharmacyId { get; set; }
		[ForeignKey("PharmacyId")]
		public Pharmacy? Pharmacy { get; set; }

		public string? MedicineName { get; set; }
		public string? PrescriptionImage { get; set; }
		public bool NeedDelivery { get; set; }

		public RequestStatus RequestStatus { get; set; } = RequestStatus.Pending;
		public string? PharmacyNotes { get; set; }

		// حقول التوصيل
		public int? VolunteerId { get; set; }
		[ForeignKey("VolunteerId")]
		public Volunteer? Volunteer { get; set; }
		
		public DeliveryStatus? DeliveryStatus { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}