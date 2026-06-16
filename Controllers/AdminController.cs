using AutoMapper;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Repositories;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
	private readonly IAdminRepository _adminRepository;
	private readonly IFileService _fileService;
	private readonly AppDbContext _context;
	private readonly INotificationService _notificationService;
	private readonly IMapper _mapper;
	private readonly ISupportRepository _supportRepository;
	private readonly UserManager<ApplicationUser> _userManager;

	public AdminController(IAdminRepository adminRepository, IFileService fileService, AppDbContext context, INotificationService notificationService, IMapper mapper, ISupportRepository supportRepository, UserManager<ApplicationUser> userManager)
	{
		_adminRepository = adminRepository;
		_fileService = fileService;
		_context = context;
		_notificationService = notificationService;
		_mapper = mapper;
		_supportRepository = supportRepository;
		_userManager = userManager;
	}

	[HttpGet("pending-users")]
	public async Task<IActionResult> GetPendingUsers()
	{
		var users = await _adminRepository.GetPendingUsersAsync();
		return Ok(users);
	}

	[HttpGet("pending-users-details")]
	[HttpGet("pending-verifications")]
	public async Task<IActionResult> GetPendingUsersDetails()
	{
		var users = (await _adminRepository.GetPendingUsersAsync()).ToList();
		var result = new List<object>();

		foreach (var u in users)
		{
			try
			{
				var role = u.UserType switch
				{
					Models.Enums.UserType.Patient => "Patient",
					Models.Enums.UserType.Doctor => "Doctor",
					Models.Enums.UserType.Pharmacist => "Pharmacy",
					Models.Enums.UserType.Volunteer => "Volunteer",
					Models.Enums.UserType.Admin => "Admin",
					_ => "Unknown"
				};

				dynamic? profileData = role switch
				{
					"Patient" => await GetPatientProfile(u.Id),
					"Doctor" => await GetDoctorProfile(u.Id),
					"Pharmacy" => await GetPharmacyProfile(u.Id),
					"Volunteer" => await GetVolunteerProfile(u.Id),
					_ => null
				};

				string? province = null;
				try { province = profileData?.governorate; } catch { }

				var entry = new
				{
					id = u.Id,
					fullName = u.FullName,
					email = u.Email,
					role,
					nationalID = u.NationalID,
					u.PhoneNumber,
					province = province ?? "N/A",
					createdAt = u.CreatedAt,
					hasCompletedProfile = u.HasCompletedProfile,
					profile = profileData
				};
				result.Add(entry);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error processing user {u.Id}: {ex.Message}");
			}
		}

		return Ok(result);
	}

	private async Task<object?> GetPatientProfile(string userId)
	{
		var p = await _context.Patients
			.Include(x => x.Governorate).Include(x => x.City)
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (p == null) return null;
		return new
		{
			p.PhoneNumber, p.Address,
			governorate = p.Governorate?.Name,
			city = p.City?.Name,
			p.HasChronicDisease, p.NIDImage, p.SocialProofImage, p.NationalID
		};
	}

	private async Task<object?> GetDoctorProfile(string userId)
	{
		var d = await _context.Doctors
			.Include(x => x.Governorate).Include(x => x.City).Include(x => x.Specialty)
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (d == null) return null;
		return new
		{
			d.PhoneNumber, d.ClinicAddress,
			governorate = d.Governorate?.Name,
			city = d.City?.Name,
			specialty = d.Specialty?.Name,
			d.NIDImage
		};
	}

	private async Task<object?> GetPharmacyProfile(string userId)
	{
		var ph = await _context.Pharmacies
			.Include(x => x.Governorate).Include(x => x.City)
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (ph == null) return null;
		return new
		{
			ph.PharmacyName, ph.PhoneNumber, ph.Address,
			governorate = ph.Governorate?.Name,
			city = ph.City?.Name,
			ph.NIDImage
		};
	}

	private async Task<object?> GetVolunteerProfile(string userId)
	{
		var v = await _context.Volunteers
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (v == null) return null;
		return new
		{
			v.PhoneNumber, v.NationalID,
			governorate = (await _context.Governorates.FindAsync(v.GovernorateId))?.Name,
			v.NIDImage
		};
	}

	[HttpPost("verify-user")]
	public async Task<IActionResult> VerifyUser([FromBody] UserVerificationDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var ok = await _adminRepository.VerifyUserAsync(dto);
		if (!ok) return BadRequest("Unable to verify user.");

		return Ok();
	}

	[HttpPost("create-case")]
	[RequestSizeLimit(10_000_000)]
	public async Task<IActionResult> CreateCase([FromForm] CreateDonationCaseDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var imageUrl = await _fileService.SaveImageAsync(dto.CaseImage, "Uploads/Cases");
		var donationCase = await _adminRepository.CreateDonationCaseAsync(dto, imageUrl);

		// إشعار لجميع المستخدمين بوجود حالة تبرع جديدة
		await _notificationService.BroadcastAsync("حالة تبرع جديدة", $"تمت إضافة حالة تبرع جديدة: {dto.Title}. ساهم معنا فى فعل الخير.");

		return Ok(donationCase);
	}

	[HttpPost("broadcast-notification")]
	[HttpPost("send-notification")]
	public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		if (string.IsNullOrEmpty(dto.TargetRole) || dto.TargetRole.Equals("All", StringComparison.OrdinalIgnoreCase))
		{
			await _notificationService.BroadcastAsync(dto.Title, dto.FinalMessage);
		}
		else
		{
			if (Enum.TryParse<Enums.UserType>(dto.TargetRole, true, out var role))
			{
				await _notificationService.SendToRoleAsync(role, dto.Title, dto.FinalMessage);
			}
			else
			{
				return BadRequest("Invalid target role.");
			}
		}

		return Ok(new { message = "Notification broadcasted successfully." });
	}

	[HttpGet("all-reports")]
	public async Task<IActionResult> GetAllReports()
	{
		var reports = await _adminRepository.GetAllReportsAsync();
		var result = _mapper.Map<IEnumerable<UserReportDto>>(reports);
		return Ok(result);
	}

	[HttpPut("resolve-report")]
	public async Task<IActionResult> ResolveReport([FromBody] ResolveReportDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var ok = await _adminRepository.ResolveReportAsync(dto);
		if (!ok) return BadRequest("Report not found.");

		return Ok();
	}

	/// <summary>
	/// طلبات متعثرة (تخطت زمن معين) من الاستشارات والأدوية.
	/// </summary>
	[HttpGet("stalled-requests")]
	public async Task<IActionResult> GetStalledRequests([FromQuery] int minutes = 60)
	{
		var stalled = await _adminRepository.GetStalledRequestsAsync(TimeSpan.FromMinutes(minutes));
		return Ok(stalled);
	}

	/// <summary>
	/// تفاصيل طلب محدد (Medical أو Medicine) مع بيانات المريض والأطراف المحتملة.
	/// </summary>
	[HttpGet("request-detail")]
	public async Task<IActionResult> GetRequestDetail([FromQuery] string type, [FromQuery] int id)
	{
		var detail = await _adminRepository.GetRequestDetailAsync(type, id);
		if (detail is null) return NotFound();
		return Ok(detail);
	}

	/// <summary>
	/// حظر مستخدم نهائياً (Ban) باستخدام الـ UserId.
	/// </summary>
	[HttpPost("ban-user/{userId}")]
	public async Task<IActionResult> BanUser(string userId)
	{
		var ok = await _adminRepository.BanUserAsync(userId);
		if (!ok) return BadRequest("Unable to ban user.");
		return Ok();
	}

	/// <summary>
	/// فك الحظر عن مستخدم (Unban) باستخدام الـ UserId.
	/// </summary>
	[HttpPost("unban-user/{userId}")]
	public async Task<IActionResult> UnbanUser(string userId)
	{
		var ok = await _adminRepository.UnbanUserAsync(userId);
		if (!ok) return BadRequest("Unable to unban user.");
		return Ok();
	}

	[HttpDelete("donation-cases/{id}")]
	public async Task<IActionResult> DeleteDonationCase(int id)
	{
		var ok = await _adminRepository.DeleteDonationCaseAsync(id);
		if (!ok) return NotFound();
		return Ok();
	}

	[HttpGet("all-users")]
	public async Task<IActionResult> GetAllUsers()
	{
		var users = (await _adminRepository.GetAllUsersAsync()).ToList();
		var result = new List<object>();

		foreach (var u in users)
		{
			try
			{
				var role = u.UserType switch
				{
					Models.Enums.UserType.Patient => "patient",
					Models.Enums.UserType.Doctor => "doctor",
					Models.Enums.UserType.Pharmacist => "pharmacy",
					Models.Enums.UserType.Volunteer => "volunteer",
					Models.Enums.UserType.Admin => "admin",
					_ => "unknown"
				};

				dynamic? profileData = role switch
				{
					"patient" => await GetPatientProfile(u.Id),
					"doctor" => await GetDoctorProfile(u.Id),
					"pharmacy" => await GetPharmacyProfile(u.Id),
					"volunteer" => await GetVolunteerProfile(u.Id),
					_ => null
				};

				string? province = null;
				string? city = null;
				string? address = null;
				try { province = profileData?.governorate; } catch { }
				try { city = profileData?.city; } catch { }
				try { address = profileData?.address; } catch { }

				var entry = new
				{
					id = u.Id,
					fullName = u.FullName,
					email = u.Email,
					role,
					nationalID = u.NationalID,
					phoneNumber = u.PhoneNumber,
					province = province ?? "N/A",
					city = city,
					address = address,
					createdAt = u.CreatedAt,
					hasCompletedProfile = u.HasCompletedProfile,
					isVerified = u.IsVerified,
					accountStatus = u.AccountStatus.ToString(),
					profile = profileData
				};
				result.Add(entry);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error processing user {u.Id}: {ex.Message}");
			}
		}

		return Ok(result);
	}

	[HttpDelete("delete-user/{userId}")]
	public async Task<IActionResult> DeleteUser(string userId)
	{
		var ok = await _adminRepository.DeleteUserAsync(userId);
		if (!ok) return BadRequest("Unable to delete user.");
		return Ok();
	}

	[HttpGet("support-messages")]
	public async Task<IActionResult> GetAllSupportMessages()
	{
		var messages = await _adminRepository.GetSupportMessagesAsync();
		return Ok(messages);
	}

	[HttpPost("reply-support")]
	public async Task<IActionResult> ReplyToSupportMessage([FromBody] SupportReplyDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var ok = await _adminRepository.ReplyToSupportMessageAsync(dto.MessageId, dto.Reply);
		if (!ok) return BadRequest("Support message not found.");

		return Ok(new { message = "Reply sent successfully." });
	}

	[HttpGet("support-tickets")]
	public async Task<IActionResult> GetAllSupportTickets()
	{
		var tickets = await _supportRepository.GetAllTicketsAsync();
		
		var result = new List<SupportTicketReturnDto>();
		foreach(var t in tickets)
		{
			var user = await _userManager.FindByIdAsync(t.UserId);
			result.Add(new SupportTicketReturnDto
			{
				Id = t.Id,
				UserId = t.UserId,
				TargetUserFullName = user?.FullName ?? "Unknown",
				UserType = t.UserType.ToString(),
				Subject = t.Subject,
				Message = t.Message,
				Status = t.Status.ToString(),
				CreatedAt = t.CreatedAt,
				Replies = t.Replies?.Select(r => new SupportReplyReturnDto
				{
					Id = r.Id,
					TicketId = r.TicketId,
					SenderId = r.SenderId,
					SenderName = "", // SenderName is computed directly in the Flutter UI using SenderType
					SenderType = r.SenderType,
					Message = r.Message,
					CreatedAt = r.CreatedAt
				}).ToList() ?? new List<SupportReplyReturnDto>()
			});
		}
		return Ok(result);
	}

	[HttpPost("reply")]
	public async Task<IActionResult> AdminReplyTicket([FromQuery] int ticketId, [FromBody] ReplySupportTicketDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		
		var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

		var reply = await _supportRepository.ReplyToTicketAsync(ticketId, adminId ?? "Admin", "Admin", dto.Message);
		if (reply == null) return NotFound("Ticket not found.");

		return Ok(new { message = "Reply sent successfully", id = reply.Id });
	}

	[HttpPost("close-ticket")]
	public async Task<IActionResult> AdminCloseTicket([FromQuery] int ticketId)
	{
		var ok = await _supportRepository.CloseTicketAsync(ticketId);
		if (!ok) return NotFound("Ticket not found.");

		return Ok(new { message = "Ticket closed successfully" });
	}

	[HttpGet("completed-services")]
	public async Task<IActionResult> GetCompletedServices([FromQuery] int? governorateId)
	{
		var response = await _adminRepository.GetCompletedServicesAsync(governorateId);
		return Ok(response);
	}

	[HttpDelete("delete-support-message/{id}")]
	public async Task<IActionResult> DeleteSupportMessage(int id)
	{
		var msg = await _context.SupportMessages.FindAsync(id);
		if (msg == null) return NotFound();
		_context.SupportMessages.Remove(msg);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Message deleted successfully." });
	}

	[HttpDelete("delete-support-ticket/{id}")]
	public async Task<IActionResult> DeleteSupportTicket(int id)
	{
		var ticket = await _context.SupportTickets.FindAsync(id);
		if (ticket == null) return NotFound();
		_context.SupportTickets.Remove(ticket);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Ticket deleted successfully." });
	}

	// =====================================================
	// ============ رعاية بوكس (R3AIA Box) ================
	// =====================================================

	/// <summary>جلب بيانات رعاية بوكس الحالية (للأدمن)</summary>
	[HttpGet("r3aia-box")]
	public async Task<IActionResult> GetR3aiaBoxSettings()
	{
		var setting = await _context.R3aiaBoxSettings
			.Include(s => s.Images.OrderBy(i => i.DisplayOrder))
			.FirstOrDefaultAsync();

		if (setting == null)
		{
			// إنشاء سجل افتراضي
			setting = new R3aiaBoxSetting
			{
				Price = 0,
				AvailableQuantity = 0,
				ShortDescription = "جهاز رعاية بوكس",
				DetailedDescription = "",
				AppDownloadLink = "",
				IsActive = false,
				UpdatedAt = DateTime.Now
			};
			_context.R3aiaBoxSettings.Add(setting);
			await _context.SaveChangesAsync();
		}

		return Ok(new R3aiaBoxInfoDto
		{
			Price = setting.Price,
			AvailableQuantity = setting.AvailableQuantity,
			ShortDescription = setting.ShortDescription,
			DetailedDescription = setting.DetailedDescription,
			AppDownloadLink = setting.AppDownloadLink,
			WhatsAppOrderNumber = setting.WhatsAppOrderNumber,
			IsActive = setting.IsActive,
			Images = setting.Images.Select(i => new R3aiaBoxImageDto
			{
				Id = i.Id,
				ImageUrl = i.ImageUrl,
				DisplayOrder = i.DisplayOrder
			}).ToList()
		});
	}

	/// <summary>تعديل إعدادات رعاية بوكس (سعر، كمية، وصف، لينك)</summary>
	[HttpPut("r3aia-box")]
	public async Task<IActionResult> UpdateR3aiaBoxSettings([FromBody] UpdateR3aiaBoxDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var setting = await _context.R3aiaBoxSettings.FirstOrDefaultAsync();
		if (setting == null)
		{
			setting = new R3aiaBoxSetting();
			_context.R3aiaBoxSettings.Add(setting);
		}

		setting.Price = dto.Price;
		setting.AvailableQuantity = dto.AvailableQuantity;
		setting.ShortDescription = dto.ShortDescription;
		setting.DetailedDescription = dto.DetailedDescription;
		setting.AppDownloadLink = dto.AppDownloadLink;
		setting.WhatsAppOrderNumber = dto.WhatsAppOrderNumber;
		setting.IsActive = dto.IsActive;
		setting.UpdatedAt = DateTime.Now;

		await _context.SaveChangesAsync();
		return Ok(new { message = "تم تحديث بيانات رعاية بوكس بنجاح." });
	}

	/// <summary>رفع صورة جديدة لرعاية بوكس</summary>
	[HttpPost("r3aia-box/images")]
	[RequestSizeLimit(10_000_000)]
	public async Task<IActionResult> UploadR3aiaBoxImage(IFormFile image)
	{
		if (image == null || image.Length == 0)
			return BadRequest("يجب رفع صورة.");

		var setting = await _context.R3aiaBoxSettings.FirstOrDefaultAsync();
		if (setting == null)
		{
			setting = new R3aiaBoxSetting { UpdatedAt = DateTime.Now };
			_context.R3aiaBoxSettings.Add(setting);
			await _context.SaveChangesAsync();
		}

		var imageUrl = await _fileService.SaveImageAsync(image, "Uploads/R3aiaBox");
		var maxOrder = await _context.R3aiaBoxImages
			.Where(i => i.R3aiaBoxSettingId == setting.Id)
			.MaxAsync(i => (int?)i.DisplayOrder) ?? 0;

		var boxImage = new R3aiaBoxImage
		{
			R3aiaBoxSettingId = setting.Id,
			ImageUrl = imageUrl,
			DisplayOrder = maxOrder + 1
		};

		_context.R3aiaBoxImages.Add(boxImage);
		await _context.SaveChangesAsync();

		return Ok(new R3aiaBoxImageDto
		{
			Id = boxImage.Id,
			ImageUrl = boxImage.ImageUrl,
			DisplayOrder = boxImage.DisplayOrder
		});
	}

	/// <summary>حذف صورة من صور رعاية بوكس</summary>
	[HttpDelete("r3aia-box/images/{imageId}")]
	public async Task<IActionResult> DeleteR3aiaBoxImage(int imageId)
	{
		var image = await _context.R3aiaBoxImages.FindAsync(imageId);
		if (image == null) return NotFound();

		_context.R3aiaBoxImages.Remove(image);
		await _context.SaveChangesAsync();
		return Ok(new { message = "تم حذف الصورة بنجاح." });
	}

	// =====================================================
	// ============ طلبات رعاية بوكس (Orders) =============
	// =====================================================

	[HttpGet("r3aia-box-orders")]
	public async Task<IActionResult> GetBoxOrders()
	{
		var orders = await _context.R3aiaBoxOrders
			.Include(o => o.User)
			.OrderByDescending(o => o.OrderDate)
			.Select(o => new {
				o.Id,
				o.FullName,
				o.PhoneNumber,
				o.Address,
				o.Status,
				o.Price,
				o.OrderDate,
				UserId = o.UserId,
				UserEmail = o.User.Email
			})
			.ToListAsync();

		return Ok(orders);
	}

	[HttpPut("r3aia-box-orders/{id}/status")]
	public async Task<IActionResult> UpdateBoxOrderStatus(int id, [FromBody] UpdateBoxOrderStatusDto dto)
	{
		var order = await _context.R3aiaBoxOrders.FindAsync(id);
		if (order == null) return NotFound("Order not found");

		order.Status = dto.Status;
		await _context.SaveChangesAsync();

		// Add notification to the user about status change
		_context.Notifications.Add(new Notification
		{
			UserId = order.UserId,
			Title = "تحديث حالة طلب رعاية بوكس",
			Message = $"تم تحديث حالة طلبك لرعاية بوكس إلى: {dto.Status}",
			IsRead = false,
			CreatedAt = DateTime.Now
		});

		await _context.SaveChangesAsync();

		return Ok(new { message = "تم تحديث حالة الطلب بنجاح" });
	}
}

public class UpdateBoxOrderStatusDto
{
	public string Status { get; set; } = string.Empty;
}
