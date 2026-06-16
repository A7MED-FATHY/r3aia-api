using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	/// <summary>
	/// حجز مريض عند طبيب الخير
	/// </summary>
	public class KhairBooking
	{
		public int Id { get; set; }

		public int PatientId { get; set; }
		[ForeignKey("PatientId")]
		public virtual Patient Patient { get; set; } = null!;

		public int KhairDoctorId { get; set; }
		[ForeignKey("KhairDoctorId")]
		public virtual KhairDoctor KhairDoctor { get; set; } = null!;

		public int SlotId { get; set; }
		[ForeignKey("SlotId")]
		public virtual KhairAppointmentSlot Slot { get; set; } = null!;

		/// <summary>ملاحظات المريض عند الحجز</summary>
		public string? PatientNotes { get; set; }

		public KhairBookingStatus Status { get; set; } = KhairBookingStatus.Pending;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		/// <summary>تقييم المريض للطبيب بعد الكشف (1-5)</summary>
		public int? PatientRating { get; set; }
	}
}
