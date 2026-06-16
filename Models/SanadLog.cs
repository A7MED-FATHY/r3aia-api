using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	/// <summary>
	/// سجل أحداث منبه سند لكل مريض.
	/// </summary>
	public class SanadLog
	{
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; } = null!;
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		/// <summary>
		/// وقت إطلاق التنبيه.
		/// </summary>
		public DateTime TriggerTime { get; set; } = DateTime.Now;

		/// <summary>
		/// هل استجاب المريض بضغط "أنا بخير".
		/// </summary>
		public bool ResponseReceived { get; set; } = false;

		/// <summary>
		/// وقت استجابة المريض (null = لم يستجب).
		/// </summary>
		public DateTime? ResponseTime { get; set; }

		/// <summary>
		/// هل تم تفعيل سيناريو الطوارئ الكامل.
		/// </summary>
		public bool EmergencyActivated { get; set; } = false;

		/// <summary>
		/// خط العرض عند التفعيل (GPS).
		/// </summary>
		public double? Latitude { get; set; }

		/// <summary>
		/// خط الطول عند التفعيل (GPS).
		/// </summary>
		public double? Longitude { get; set; }
	}
}
