using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R3AIA.DTOs;

public class CompletePatientProfileDto
{
	public string FullName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string Address { get; set; } = string.Empty;

	public bool HasChronicDisease { get; set; }

	[Required]
	public IFormFile NIDImage { get; set; } = null!;

	[Required]
	public IFormFile SocialProofImage { get; set; } = null!;
}

public class UpdatePatientProfileDto
{
	public string FullName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }

	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string Address { get; set; } = string.Empty;

	public bool HasChronicDisease { get; set; }
}

public class CompleteDoctorProfileDto
{
	public string FullName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int SpecialtyId { get; set; }

	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string ClinicAddress { get; set; } = string.Empty;

	public IFormFile? NIDImage { get; set; }
	public IFormFile? ProfileImage { get; set; }

	public string? BioNotes { get; set; }

	public Models.Enums.ConsultationType ConsultationType { get; set; } = Models.Enums.ConsultationType.Free;

	public decimal? DiscountedPrice { get; set; }
	public decimal? RegularPrice { get; set; }
}

public class CompletePharmacyProfileDto
{
	public string PharmacyName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string Address { get; set; } = string.Empty;

	[Required]
	public IFormFile NIDImage { get; set; } = null!;
}

public class CompleteVolunteerProfileDto
{
	[Required]
	public string FullName { get; set; } = string.Empty;

	public string NationalID { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string Address { get; set; } = string.Empty;

	[Required]
	public IFormFile NIDImage { get; set; } = null!;
}

