using System.ComponentModel.DataAnnotations;
using static R3AIA.Models.Enums;

namespace R3AIA.DTOs;

// ── PatientCase DTOs ─────────────────────────────────────────────────────────

public class PatientCaseSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CaseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal RequiredAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public double ProgressPercent { get; set; }
    public List<string> Images { get; set; } = new();
    public string? GovernorateName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsCompleted => CollectedAmount >= RequiredAmount;
}

public class PatientCaseDetailDto : PatientCaseSummaryDto
{
    public List<PatientDonationSummaryDto> RecentDonations { get; set; } = new();
}

public class CreatePatientCaseDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? GovernorateId { get; set; }

    [Required]
    public CaseType CaseType { get; set; } = CaseType.Treatment;

    [Required, Range(1, double.MaxValue, ErrorMessage = "المبلغ المطلوب يجب أن يكون أكبر من صفر")]
    public decimal RequiredAmount { get; set; }

    public DateTime? ExpiryDate { get; set; }

    /// <summary>Up to 5 images</summary>
    public List<IFormFile>? Images { get; set; }
}

public class UpdatePatientCaseDto
{
    [MaxLength(200)]
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? GovernorateId { get; set; }
    public CaseType? CaseType { get; set; }
    public decimal? RequiredAmount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<IFormFile>? Images { get; set; }
}

// ── PatientDonation DTOs ─────────────────────────────────────────────────────

public class PatientDonationSummaryDto
{
    public int Id { get; set; }
    public int PatientCaseId { get; set; }
    public string CaseTitle { get; set; } = string.Empty;
    public string DonorDisplayName { get; set; } = string.Empty;
    public string? DonorPhone { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ProofImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreatePatientDonationDto
{
    [Required]
    public int PatientCaseId { get; set; }

    [Required, Range(1, double.MaxValue, ErrorMessage = "لا يمكن التبرع بمبلغ صفر أو أقل")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.InstaPay;

    /// <summary>Optional: filled only for guest donations</summary>
    public string? DonorName { get; set; }

    /// <summary>Optional: filled only for guest donations</summary>
    public string? DonorPhone { get; set; }

    /// <summary>Optional proof/receipt screenshot</summary>
    public IFormFile? ProofImage { get; set; }
}

public class AdminPatientDonationDto : PatientDonationSummaryDto
{
    public bool IsGuest { get; set; }
    public string? DonorId { get; set; }
}
