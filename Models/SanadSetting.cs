using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	/// <summary>
	/// إعدادات خدمة سند الخاصة بكل مريض.
	/// </summary>
	public class SanadSetting
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = null!;
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		/// <summary>
		/// اسم جهة الاتصال في الطوارئ.
		/// </summary>
		[Required, MaxLength(100)]
		public string EmergencyContactName { get; set; } = null!;

		/// <summary>
		/// رقم هاتف جهة الاتصال في الطوارئ.
		/// </summary>
		[Required, MaxLength(20)]
		public string EmergencyContactPhone { get; set; } = null!;

		/// <summary>
		/// طبيعة العلاقة (أب، أم، زوج، صديق، ...إلخ).
		/// </summary>
		[MaxLength(50)]
		public string? RelationshipType { get; set; }

		/// <summary>
		/// أوقات إطلاق التنبيه اليومية بصيغة JSON (مثال: ["09:00","18:00"]).
		/// </summary>
		[Required]
		public string AlertTimesJson { get; set; } = "[]";

		/// <summary>
		/// مدة الانتظار قبل تفعيل الطوارئ تلقائياً (بالثواني).
		/// </summary>
		public int DelaySeconds { get; set; } = 120;

		/// <summary>
		/// هل الخدمة مفعلة حاليًا.
		/// </summary>
		public bool IsActive { get; set; } = true;

		/// <summary>
		/// معرّف حساب الحارس (جهة الاتصال) في التطبيق، لإرسال Push Notification له مباشرةً.
		/// </summary>
		[MaxLength(450)]
		public string? CompanionUserId { get; set; }

		[ForeignKey("CompanionUserId")]
		public ApplicationUser? Companion { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime UpdatedAt { get; set; } = DateTime.Now;
	}
}
