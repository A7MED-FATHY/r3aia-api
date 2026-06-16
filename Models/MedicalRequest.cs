using R3AIA.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	public class MedicalRequest
	{
		public int Id { get; set; }

		public int PatientId { get; set; }
		[ForeignKey("PatientId")]
		public virtual Patient Patient { get; set; } = null!;

		public int? DoctorId { get; set; }
		[ForeignKey("DoctorId")]
		public Doctor? Doctor { get; set; }

		public int SpecialtyId { get; set; }
		public virtual Specialty Specialty { get; set; } = null!;

		public string Description { get; set; } = null!;
		public RequestStatus RequestStatus { get; set; } = RequestStatus.Pending;

		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime? AppointmentDate { get; set; }
		public string? DoctorNotes { get; set; }

		/// <summary>
		/// مسارات صور الأشعة والتحاليل المرفقة (مفصولة بفاصلة)
		/// </summary>
		public string? MedicalImages { get; set; }

		/// <summary>
		/// هل يعاني المريض من مرض مزمن؟
		/// </summary>
		public bool ChronicDisease { get; set; }

		/// <summary>
		/// هل يحتوي الطلب على مرفقات طبية؟
		/// </summary>
		public bool HasAttachments { get; set; } = false;
	}
}