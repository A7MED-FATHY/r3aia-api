using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	/// <summary>
	/// ملف طبيب الخير — One-to-One مع Doctor
	/// يُخزن إعدادات الطبيب للمشاركة في برنامج الكشف المجاني/المخفض
	/// </summary>
	public class KhairDoctor
	{
		public int Id { get; set; }

		// FK إلى جدول الأطباء الحالي (One-to-One)
		public int DoctorId { get; set; }
		[ForeignKey("DoctorId")]
		public virtual Doctor Doctor { get; set; } = null!;

		/// <summary>نوع الكشف: مجاني / مخفض / عادي</summary>
		public ConsultationType ConsultationType { get; set; } = ConsultationType.Free;

		/// <summary>سعر الكشف المخفض (فقط عند ConsultationType = Discounted)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? DiscountedPrice { get; set; }

		/// <summary>سعر الكشف العادي (لعرضه مشطوباً قبل الخصم)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? RegularPrice { get; set; }

		/// <summary>الحد الأقصى للحالات المجانية يومياً</summary>
		public int FreeDailyLimit { get; set; } = 3;

		/// <summary>نبذة شخصية عن الطبيب</summary>
		public string? BioNotes { get; set; }

		/// <summary>متوسط تقييم المرضى (0–5)</summary>
		[Column(TypeName = "decimal(3,2)")]
		public decimal Rating { get; set; } = 0;

		/// <summary>عدد التقييمات الكلية</summary>
		public int RatingCount { get; set; } = 0;

		/// <summary>إجمالي الكشوفات المجانية (لنظام النقاط)</summary>
		public int TotalFreeConsultations { get; set; } = 0;

		/// <summary>هل الطبيب نشط في البرنامج؟</summary>
		public bool IsActive { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public virtual ICollection<KhairAppointmentSlot> Slots { get; set; } = [];
		public virtual ICollection<KhairBooking> Bookings { get; set; } = [];
	}
}
