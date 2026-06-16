using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class CreateReportDto
{
	[Required]
	public string ReportedUserId { get; set; } = string.Empty;

	[Required]
	public string Reason { get; set; } = string.Empty;
}

public class NotificationDto
{
	public int Id { get; set; }
	public string Message { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public bool IsRead { get; set; }
}

public class ContactMessageDto
{
	[Required]
	public string FullName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Category { get; set; } = string.Empty;

	[Required]
	public string Message { get; set; } = string.Empty;
}
