using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class UserVerificationDto
{
	[Required]
	public string UserId { get; set; } = string.Empty;

	[Required]
	public bool IsApproved { get; set; }
}


