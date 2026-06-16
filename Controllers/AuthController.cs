using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Repositories;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IAccountRepository _accountRepository;
	private readonly ILogger<AuthController> _logger;

	public AuthController(IAccountRepository accountRepository, ILogger<AuthController> logger)
	{
		_accountRepository = accountRepository;
		_logger = logger;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var repo = _accountRepository as AccountRepository;
			var result = await _accountRepository.RegisterAsync(dto);
			if (result is null)
			{
				var errors = repo?.Errors ?? new[] { "Registration failed" };
				return BadRequest(new { errors });
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during registration for {Email}", dto.Email);
			return StatusCode(500, new { message = "Internal server error occurred during registration" });
		}
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var result = await _accountRepository.LoginAsync(dto);
			if (result is null) return Unauthorized();

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during login for {Email}", dto.Email);
			return StatusCode(500, new { message = "Internal server error occurred during login" });
		}
	}

	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var token = await _accountRepository.ForgotPasswordAsync(dto);
			if (token == null)
			{
				var repo = _accountRepository as AccountRepository;
				var errors = repo?.Errors ?? new[] { "Email not found" };
				return BadRequest(new { errors });
			}

			return Ok(new { token, message = "Password reset token generated successfully. In production, this would be emailed." });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing forgot password for {Email}", dto.Email);
			return StatusCode(500, new { message = "An internal error occurred." });
		}
	}

	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var success = await _accountRepository.ResetPasswordAsync(dto);
			if (!success)
			{
				var repo = _accountRepository as AccountRepository;
				var errors = repo?.Errors ?? new[] { "Password reset failed" };
				return BadRequest(new { errors });
			}

			return Ok(new { message = "Password reset successfully." });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error resetting password for {Email}", dto.Email);
			return StatusCode(500, new { message = "An internal error occurred." });
		}
	}
}


