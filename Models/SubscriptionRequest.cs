using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	/// <summary>
	/// طلب اشتراك المريض في خدمة سند المدفوعة.
	/// </summary>
	public class SubscriptionRequest
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = null!;
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		public SubscriptionPaymentMethod PaymentMethod { get; set; }

		/// <summary>
		/// مسار صورة إثبات الدفع (Vodafone Cash / InstaPay).
		/// </summary>
		public string? ScreenshotPath { get; set; }

		public SubscriptionRequestStatus Status { get; set; } = SubscriptionRequestStatus.Pending;

		public DateTime RequestedAt { get; set; } = DateTime.Now;

		public DateTime? ReviewedAt { get; set; }

		/// <summary>
		/// الأدمن الذي راجع الطلب.
		/// </summary>
		public string? ReviewedByAdminId { get; set; }

		/// <summary>
		/// ملاحظات عند الرفض.
		/// </summary>
		public string? RejectionNotes { get; set; }

		/// <summary>
		/// مدة الاشتراك بالأيام (الافتراضي 30 يوم).
		/// </summary>
		public int DurationDays { get; set; } = 30;
	}
}
