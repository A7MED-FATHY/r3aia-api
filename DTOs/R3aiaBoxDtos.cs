using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs
{
	/// <summary>DTO لعرض بيانات رعاية بوكس (للمستخدم العادي)</summary>
	public class R3aiaBoxInfoDto
	{
		public decimal Price { get; set; }
		public int AvailableQuantity { get; set; }
		public string ShortDescription { get; set; } = string.Empty;
		public string DetailedDescription { get; set; } = string.Empty;
		public string AppDownloadLink { get; set; } = string.Empty;
		public string WhatsAppOrderNumber { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public List<R3aiaBoxImageDto> Images { get; set; } = new();
	}

	/// <summary>DTO لصورة البوكس</summary>
	public class R3aiaBoxImageDto
	{
		public int Id { get; set; }
		public string ImageUrl { get; set; } = string.Empty;
		public int DisplayOrder { get; set; }
	}

	/// <summary>DTO لتحديث إعدادات رعاية بوكس (للأدمن)</summary>
	public class UpdateR3aiaBoxDto
	{
		[Range(0, 999999)]
		public decimal Price { get; set; }

		[Range(0, 99999)]
		public int AvailableQuantity { get; set; }

		[Required]
		public string ShortDescription { get; set; } = string.Empty;

		public string DetailedDescription { get; set; } = string.Empty;

		public string AppDownloadLink { get; set; } = string.Empty;

		public string WhatsAppOrderNumber { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true;
	}
}
