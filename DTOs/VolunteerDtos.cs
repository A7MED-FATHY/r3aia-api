using System.ComponentModel.DataAnnotations;
using static R3AIA.Models.Enums;

namespace R3AIA.DTOs;

public class CreateVolunteerRequestDto
{
	[Required]
	public VolunteerRequestType Type { get; set; }

	[Required]
	public string Description { get; set; } = string.Empty;
}

public class VolunteerRequestSummaryDto
{
	public int Id { get; set; }
	public string PatientName { get; set; } = string.Empty;
	public string PatientPhone { get; set; } = string.Empty;
	public string PatientAddress { get; set; } = string.Empty;
	public string PatientCity { get; set; } = string.Empty;
	public string PatientGovernorate { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public decimal? Amount { get; set; }
	public string? PaymentMethod { get; set; }
	public string Status { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
}

public class MyVolunteerRequestDto
{
	public int Id { get; set; }
	public string Type { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public decimal? Amount { get; set; }
	public string? PaymentMethod { get; set; }
	public string Status { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }

	// معلومات المتطوع (إذا تم القبول)
	public string? VolunteerName { get; set; }
	public string? VolunteerPhone { get; set; }
}

public class FulfillFinancialDonationDto
{
	[Required]
	public decimal Amount { get; set; }

	[Required]
	public PaymentMethod PaymentMethod { get; set; }

    public IFormFile? ReceiptImage { get; set; }
}
