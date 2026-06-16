using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class CreateMedicineRequestDto
{
	public string? MedicineName { get; set; }
	public IFormFile? PrescriptionImage { get; set; }

	/// <summary>
	/// هل يحتاج الطلب لتوصيل بواسطة متطوع؟
	/// </summary>
	public bool NeedDelivery { get; set; }
}

public class MedicineRequestSummaryDto
{
	public int Id { get; set; }
	public string PatientName { get; set; } = string.Empty;
	public string PatientPhone { get; set; } = string.Empty;
	public string PatientAddress { get; set; } = string.Empty;
	public string PatientCity { get; set; } = string.Empty;
	public string PatientGovernorate { get; set; } = string.Empty;
	public string? MedicineName { get; set; }
	public string? PrescriptionImageUrl { get; set; }
	public string Status { get; set; } = string.Empty;
	public bool NeedDelivery { get; set; }
	public DateTime CreatedAt { get; set; }
}

/// <summary>
/// رد الصيدلية على طلب دواء
/// </summary>
public class RespondToMedicineRequestDto
{
	/// <summary>
	/// ملاحظات الصيدلية للمريض (اختياري)
	/// </summary>
	public string? PharmacyNotes { get; set; }
}

/// <summary>
/// طلب دواء خاص بالمريض مع تفاصيل الصيدلية
/// </summary>
public class MyMedicineRequestDto
{
	public int Id { get; set; }
	public string? MedicineName { get; set; }
	public string? PrescriptionImageUrl { get; set; }
	public bool NeedDelivery { get; set; }
	public string Status { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	
	// معلومات الصيدلية (إذا تم القبول)
	public string? PharmacyName { get; set; }
	public string? PharmacyPhone { get; set; }
	public string? PharmacyAddress { get; set; }
	public string? PharmacyNotes { get; set; }
}

/// <summary>
/// مهمة توصيل للمتطوعين
/// </summary>
public class DeliveryTaskDto
{
	public int RequestId { get; set; }
	
	// عنوان الصيدلية
	public string PharmacyName { get; set; } = string.Empty;
	public string PharmacyAddress { get; set; } = string.Empty;
	public string PharmacyPhone { get; set; } = string.Empty;
	
	// عنوان المريض
	public string PatientName { get; set; } = string.Empty;
	public string PatientAddress { get; set; } = string.Empty;
	public string PatientPhone { get; set; } = string.Empty;
	
	public DateTime CreatedAt { get; set; }
}
