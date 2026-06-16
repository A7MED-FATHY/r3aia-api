using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;
using static R3AIA.Models.Enums;

namespace R3AIA.Repositories;

public interface IAccountRepository
{
	Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
	Task<AuthResponseDto?> LoginAsync(LoginDto dto);
	Task<string?> ForgotPasswordAsync(ForgotPasswordDto dto);
	Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
}

public class AccountRepository : IAccountRepository
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly SignInManager<ApplicationUser> _signInManager;
	private readonly IJwtService _jwtService;

	public string[] Errors { get; set; } = Array.Empty<string>();

	public AccountRepository(
		UserManager<ApplicationUser> userManager,
		SignInManager<ApplicationUser> signInManager,
		IJwtService jwtService)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_jwtService = jwtService;
	}

	public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
	{
		// منع التسجيل لو الرقم القومي محظور مسبقاً
		var bannedUser = await _userManager.Users
			.FirstOrDefaultAsync(u => u.NationalID == dto.NationalID && u.AccountStatus == Enums.AccountStatus.Banned);
		if (bannedUser != null)
		{
			Errors = new[] { "This National ID is banned." };
			return null;
		}

		// التحقق من أن الرقم القومي غير مسجل مسبقاً
		var existingUserByNationalId = await _userManager.Users
			.AnyAsync(u => u.NationalID == dto.NationalID);
		if (existingUserByNationalId)
		{
			Errors = new[] { "This National ID is already registered." };
			return null;
		}

		// التحقق من البريد الإلكتروني
		var existingByEmail = await _userManager.Users
			.FirstOrDefaultAsync(u => u.Email == dto.Email);
		if (existingByEmail != null)
		{
			Errors = new[] { "This email is already in use." };
			return null;
		}

		// التحقق من اسم المستخدم
		var existingByUsername = await _userManager.Users
			.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
		if (existingByUsername != null)
		{
			Errors = new[] { "This username is already in use." };
			return null;
		}

		// تحويل اسم الدور المرسل من الفرونت-إند لاسم الـ Enum
		// الفرونت يرسل "Pharmacy" لكن الـ Enum هو "Pharmacist"
		var roleMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Patient"] = "Patient",
			["Doctor"] = "Doctor",
			["Pharmacy"] = "Pharmacist",
			["Pharmacist"] = "Pharmacist",
			["Volunteer"] = "Volunteer",
			["Admin"] = "Admin",
			["Companion"] = "Companion",
			["Premium"] = "Premium",
		};

		var normalizedRole = roleMapping.TryGetValue(dto.Role ?? "", out var mapped) ? mapped : "Patient";

		UserType userType = UserType.Patient;
		if (Enum.TryParse<UserType>(normalizedRole, true, out var parsedType))
		{
			userType = parsedType;
		}

		bool isPremium = userType == UserType.Premium;

		var user = new ApplicationUser
		{
			Email = dto.Email,
			UserName = dto.UserName,
			FullName = dto.FullName,
			NationalID = dto.NationalID,
			UserType = userType,
			AccountStatus = isPremium ? Enums.AccountStatus.Active : Enums.AccountStatus.Pending,
			IsVerified = isPremium,
			HasCompletedProfile = isPremium,
			PhoneNumber = dto.PhoneNumber
		};

		var result = await _userManager.CreateAsync(user, dto.Password);
		if (!result.Succeeded)
		{
			Errors = result.Errors.Select(e => e.Description).ToArray();
			return null;
		}

		// الـ Identity Role يستخدم نفس اسم الـ Enum (Pharmacist مش Pharmacy)
		await _userManager.AddToRoleAsync(user, normalizedRole);

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

		return new AuthResponseDto
		{
			Token = token,
			UserId = user.Id,
			FullName = user.FullName,
			Role = roles.FirstOrDefault() ?? string.Empty,
			IsVerified = user.IsVerified,
			HasCompletedProfile = user.HasCompletedProfile,
			AccountStatus = user.AccountStatus.ToString(),
			PhoneNumber = user.PhoneNumber ?? string.Empty,
			NationalID = user.NationalID ?? string.Empty,
			Email = user.Email ?? string.Empty
		};
	}

	public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
	{
		// استخدام queryable مباشر لتجنب الـ crash لو في إيميلات مكررة في الداتابيز
		var user = await _userManager.Users
			.FirstOrDefaultAsync(u => u.Email == dto.Email);
		
		if (user is null) return null;

		// منع تسجيل الدخول للحسابات المحظورة نهائياً
		if (user.AccountStatus == Enums.AccountStatus.Banned)
		{
			return null;
		}

		var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
		if (!result.Succeeded) return null;

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

		return new AuthResponseDto
		{
			Token = token,
			UserId = user.Id,
			FullName = user.FullName,
			Role = roles.FirstOrDefault() ?? string.Empty,
			IsVerified = user.IsVerified,
			HasCompletedProfile = user.HasCompletedProfile,
			AccountStatus = user.AccountStatus.ToString(),
			PhoneNumber = user.PhoneNumber ?? string.Empty,
			NationalID = user.NationalID ?? string.Empty,
			Email = user.Email ?? string.Empty
		};
	}

	public async Task<string?> ForgotPasswordAsync(ForgotPasswordDto dto)
	{
		var user = await _userManager.FindByEmailAsync(dto.Email);
		if (user == null || user.AccountStatus == Enums.AccountStatus.Banned)
		{
			Errors = new[] { "User not found or banned." };
			return null;
		}

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		return token; // Returning token directly for frontend, normally this would be emailed
	}

	public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
	{
		var user = await _userManager.FindByEmailAsync(dto.Email);
		if (user == null)
		{
			Errors = new[] { "User not found." };
			return false;
		}

		var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
		if (!result.Succeeded)
		{
			Errors = result.Errors.Select(e => e.Description).ToArray();
			return false;
		}

		return true;
	}
}


