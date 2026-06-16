using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class RegisterDto
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string UserName { get; set; } = string.Empty;

	[Required]
	public string FullName { get; set; } = string.Empty;

	[Required]
	public string NationalID { get; set; } = string.Empty;

	[Required]
	public string Password { get; set; } = string.Empty;

	[Required]
	public string Role { get; set; } = string.Empty;

	[Required]
	public string PhoneNumber { get; set; } = string.Empty;
}

public class LoginDto
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
	public string Token { get; set; } = string.Empty;
	public string UserId { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public bool IsVerified { get; set; }
	public bool HasCompletedProfile { get; set; }
	public string AccountStatus { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public string NationalID { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string Token { get; set; } = string.Empty;

	[Required]
	[MinLength(6)]
	public string NewPassword { get; set; } = string.Empty;
}
