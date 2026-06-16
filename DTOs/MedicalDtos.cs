using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class MedicalRequestSummaryDto
{
	public int Id { get; set; }
	public string PatientName { get; set; } = string.Empty;
	public string SpecialtyName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string PatientPhone { get; set; } = string.Empty;
	public string PatientGovernorate { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public string Status { get; set; } = string.Empty;
	public bool ChronicDisease { get; set; }
	public bool HasAttachments { get; set; }
	public DateTime? AppointmentDate { get; set; }
}

/// <summary>
/// الملف الطبي الكامل للطلب (للطبيب)
/// </summary>
public class MedicalRequestDetailDto
{
	// معلومات الطلب
	public int RequestId { get; set; }
	public string SpecialtyName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public string Status { get; set; } = string.Empty;
	public bool ChronicDisease { get; set; }
	
	// بيانات المريض الشخصية
	public PatientInfoDto Patient { get; set; } = new();
	
	// المرفقات الطبية
	public List<string> MedicalImages { get; set; } = new();
	public bool HasAttachments { get; set; }
}

/// <summary>
/// معلومات المريض (للطبيب فقط)
/// </summary>
public class PatientInfoDto
{
	public string FullName { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string GovernorateName { get; set; } = string.Empty;
	public string CityName { get; set; } = string.Empty;
	public string Address { get; set; } = string.Empty;
	public bool HasChronicDisease { get; set; }
	public string? MedicalHistory { get; set; }
}

/// <summary>
/// رد الدكتور على طلب المريض
/// </summary>
public class RespondToRequestDto
{
	/// <summary>
	/// تاريخ ووقت الموعد المحدد
	/// </summary>
	[Required]
	public DateTime AppointmentDate { get; set; }
	
	/// <summary>
	/// ملاحظات الدكتور للمريض (اختياري)
	/// </summary>
	public string? DoctorNotes { get; set; }
}

/// <summary>
/// طلب استشارة خاص بالمريض مع تفاصيل الرد
/// </summary>
public class MyMedicalRequestDto
{
	public int Id { get; set; }
	public string SpecialtyName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public string Status { get; set; } = string.Empty;
	public bool ChronicDisease { get; set; }
	public bool HasAttachments { get; set; }
	
	// معلومات الدكتور (إذا تم القبول)
	public string? DoctorName { get; set; }
	public string? DoctorPhone { get; set; }
	public string? ClinicAddress { get; set; }
	
	// الرد
	public DateTime? AppointmentDate { get; set; }
	public string? DoctorNotes { get; set; }
}

/// <summary>
/// إلغاء طلب استشارة (من المريض أو الدكتور)
/// </summary>
public class CancelRequestDto
{
	/// <summary>
	/// سبب الإلغاء (اختياري)
	/// </summary>
	public string? CancellationReason { get; set; }
}


