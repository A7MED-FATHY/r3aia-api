namespace R3AIA.DTOs;

public class AdminActiveRequestDto
{
	public int Id { get; set; }
	public string Type { get; set; } = string.Empty; // Medical | Medicine
	public DateTime CreatedAt { get; set; }
	public double AgeMinutes { get; set; }
	public string Bottleneck { get; set; } = string.Empty; // Doctor | Pharmacy | Volunteer

	public string PatientName { get; set; } = string.Empty;
	public string PatientGovernorate { get; set; } = string.Empty;
	public string PatientCity { get; set; } = string.Empty;
	public string PatientPhone { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;
	public string TargetName { get; set; } = string.Empty;
	public string TargetPhone { get; set; } = string.Empty;
}

public class AdminRequestDetailDto
{
	public int Id { get; set; }
	public string Type { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; }
	public double AgeMinutes { get; set; }

	public string PatientName { get; set; } = string.Empty;
	public string PatientGovernorate { get; set; } = string.Empty;
	public string PatientCity { get; set; } = string.Empty;
	public string PatientPhone { get; set; } = string.Empty;

	// Medical
	public string? SpecialtyName { get; set; }
	public string? Description { get; set; }

	// Medicine
	public string? PrescriptionImageUrl { get; set; }

	public List<AdminContactSuggestionDto> SuggestedContacts { get; set; } = new();
}

public class AdminContactSuggestionDto
{
	public string Name { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty; // Doctor | Pharmacy
	public string PhoneNumber { get; set; } = string.Empty;
}


