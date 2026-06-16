using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	/// <summary>
	/// موعد متاح عند طبيب الخير
	/// </summary>
	public class KhairAppointmentSlot
	{
		public int Id { get; set; }

		public int KhairDoctorId { get; set; }
		[ForeignKey("KhairDoctorId")]
		public virtual KhairDoctor KhairDoctor { get; set; } = null!;

		/// <summary>تاريخ الموعد</summary>
		public DateOnly SlotDate { get; set; }

		/// <summary>وقت البداية</summary>
		public TimeOnly StartTime { get; set; }

		/// <summary>وقت النهاية</summary>
		public TimeOnly EndTime { get; set; }

		/// <summary>هل الموعد محجوز؟</summary>
		public bool IsBooked { get; set; } = false;

		// Navigation
		public virtual KhairBooking? Booking { get; set; }
	}
}
