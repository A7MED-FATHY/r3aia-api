using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class DonationCaseSummaryDto
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public decimal GoalAmount { get; set; }
	public decimal CollectedAmount { get; set; }
	public string CaseImage { get; set; } = string.Empty;
	public string PatientName { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public bool IsCompleted { get; set; }

	// Alias for CollectedAmount to match frontend currentAmount
	public decimal CurrentAmount => CollectedAmount;
}

public class CreateDonationDto
{
	[Required]
	public int CaseId { get; set; }

	[Required]
	[Range(0.01, double.MaxValue)]
	public decimal Amount { get; set; }

	public IFormFile? ReceiptImage { get; set; }
}

public class DonationResultDto
{
	public int Id { get; set; }
	public int CaseId { get; set; }
	public decimal Amount { get; set; }
	public string Status { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
}
