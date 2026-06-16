using System.ComponentModel.DataAnnotations;

namespace R3AIA.Models
{
	/// <summary>
	/// إعدادات جهاز رعاية بوكس – سجل واحد فقط يتحكم فيه الأدمن.
	/// </summary>
	public class R3aiaBoxSetting
	{
		[Key]
		public int Id { get; set; }

		/// <summary>سعر الجهاز بالجنيه المصري</summary>
		public decimal Price { get; set; }

		/// <summary>الكمية المتوفرة حالياً</summary>
		public int AvailableQuantity { get; set; }

		/// <summary>وصف مختصر للجهاز</summary>
		public string ShortDescription { get; set; } = string.Empty;

		/// <summary>وصف تفصيلي للجهاز ومميزاته</summary>
		public string DetailedDescription { get; set; } = string.Empty;

		/// <summary>لينك تحميل تطبيق رعاية بوكس</summary>
		public string AppDownloadLink { get; set; } = string.Empty;

		/// <summary>هل الجهاز معروض للبيع حالياً</summary>
		public bool IsActive { get; set; } = true;

		/// <summary>رقم واتساب لتأكيد الطلبات</summary>
		public string WhatsAppOrderNumber { get; set; } = string.Empty;

		public DateTime UpdatedAt { get; set; } = DateTime.Now;

		// Navigation
		public ICollection<R3aiaBoxImage> Images { get; set; } = new List<R3aiaBoxImage>();
	}

	/// <summary>
	/// صور جهاز رعاية بوكس (علاقة One-to-Many).
	/// </summary>
	public class R3aiaBoxImage
	{
		[Key]
		public int Id { get; set; }

		public int R3aiaBoxSettingId { get; set; }
		public R3aiaBoxSetting R3aiaBoxSetting { get; set; } = null!;

		/// <summary>مسار الصورة على السيرفر</summary>
		public string ImageUrl { get; set; } = string.Empty;

		/// <summary>ترتيب العرض</summary>
		public int DisplayOrder { get; set; }
	}
}
