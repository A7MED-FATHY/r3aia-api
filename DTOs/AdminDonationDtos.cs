using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class CreateDonationCaseDto
{
	[Required]
	public string Title { get; set; } = string.Empty;

	[Required]
	public string Description { get; set; } = string.Empty;

	[Required]
	[Range(0.01, double.MaxValue)]
	public decimal GoalAmount { get; set; }

	[Required]
	public string PatientName { get; set; } = string.Empty;

	[Required]
	public IFormFile CaseImage { get; set; } = default!;
}


