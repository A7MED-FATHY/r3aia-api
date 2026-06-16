using static R3AIA.Models.Enums;

namespace R3AIA.DTOs
{
	// ============================================================
	// DTOs للعرض (Response)
	// ============================================================

	public class KhairDoctorListDto
	{
		public int KhairDoctorId { get; set; }
		public int DoctorId { get; set; }
		public string FullName { get; set; } = null!;
		public string SpecialtyName { get; set; } = null!;
		public string GovernorateName { get; set; } = null!;
		public string? BioNotes { get; set; }
		public ConsultationType ConsultationType { get; set; }
		public decimal? DiscountedPrice { get; set; }
		public decimal? RegularPrice { get; set; }
		public decimal Rating { get; set; }
		public int RatingCount { get; set; }
		public int FreeDailyLimit { get; set; }
		public int TotalFreeConsultations { get; set; }
		public string? ProfilePictureUrl { get; set; }
		public string BadgeLevel { get; set; } = "none"; // none / bronze / silver / gold
	}

	public class KhairDoctorDetailDto : KhairDoctorListDto
	{
		public string? ClinicAddress { get; set; }
		public int AvailableSlotsCount { get; set; }
	}

	public class KhairSlotDto
	{
		public int SlotId { get; set; }
		public DateOnly SlotDate { get; set; }
		public TimeOnly StartTime { get; set; }
		public TimeOnly EndTime { get; set; }
		public bool IsBooked { get; set; }
	}

	public class KhairBookingDto
	{
		public int BookingId { get; set; }
		public string DoctorName { get; set; } = null!;
		public string SpecialtyName { get; set; } = null!;
		public string GovernorateName { get; set; } = null!;
		public ConsultationType ConsultationType { get; set; }
		public decimal? Price { get; set; }
		public DateOnly SlotDate { get; set; }
		public TimeOnly StartTime { get; set; }
		public TimeOnly EndTime { get; set; }
		public KhairBookingStatus Status { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	// ============================================================
	// DTOs للطلبات (Request)
	// ============================================================

	public class KhairSetupRequest
	{
		public ConsultationType ConsultationType { get; set; }
		public decimal? DiscountedPrice { get; set; }
		public decimal? RegularPrice { get; set; }
		public int FreeDailyLimit { get; set; } = 3;
		public string? BioNotes { get; set; }
		public Microsoft.AspNetCore.Http.IFormFile? ProfilePicture { get; set; }
	}

	public class KhairAddSlotRequest
	{
		public DateOnly SlotDate { get; set; }
		public TimeOnly StartTime { get; set; }
		public TimeOnly EndTime { get; set; }
	}

	public class KhairBookRequest
	{
		public int SlotId { get; set; }
		public string? PatientNotes { get; set; }
	}

	public class KhairUpdateStatusDto
	{
		public KhairBookingStatus Status { get; set; }
	}
}
