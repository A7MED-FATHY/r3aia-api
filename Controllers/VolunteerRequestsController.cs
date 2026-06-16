using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Repositories;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VolunteerRequestsController : ControllerBase
{
	private readonly IVolunteerRepository _volunteerRepository;
	private readonly AppDbContext _context;
	private readonly IMapper _mapper;
	private readonly INotificationService _notificationService;
	private readonly IFileService _fileService;

	public VolunteerRequestsController(
		IVolunteerRepository volunteerRepository,
		AppDbContext context,
		IMapper mapper,
		INotificationService notificationService,
		IFileService fileService)
	{
		_volunteerRepository = volunteerRepository;
		_context = context;
		_mapper = mapper;
		_notificationService = notificationService;
		_fileService = fileService;
	}

	[HttpPost("create")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> CreateRequest([FromBody] CreateVolunteerRequestDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var patient = await _context.Patients
			.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		
		if (patient is null) return BadRequest("Patient profile not found.");

		var request = await _volunteerRepository.CreateRequestAsync(patient.Id, dto.Type, dto.Description);

		return Ok(request);
	}

	[HttpGet("available")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetAvailable()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _volunteerRepository.GetAvailableRequestsForVolunteerAsync(userId);
		var result = _mapper.Map<IEnumerable<VolunteerRequestSummaryDto>>(requests);
		return Ok(result);
	}

	[HttpPost("accept/{requestId}")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> AcceptRequest(int requestId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _volunteerRepository.AcceptRequestAsync(requestId, userId);
		if (request is null) return BadRequest("Cannot accept this request.");

		// Notify patient
		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
		if (volunteer != null && request.Patient?.User != null)
		{
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				"تم قبول طلب المساعدة",
				$"قام المتطوع {volunteer.FullName} بقبول طلبك. سيتم التواصل معك قريباً."
			);
		}

		return Ok(new { message = "Request accepted successfully", status = request.Status.ToString() });
	}

	[HttpPost("fulfill-donation/{requestId}")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> FulfillDonation(int requestId, [FromForm] FulfillFinancialDonationDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		string? receiptUrl = null;
		if (dto.ReceiptImage != null)
		{
			receiptUrl = await _fileService.SaveImageAsync(dto.ReceiptImage, "Uploads/Donations");
		}

		var request = await _volunteerRepository.FulfillFinancialDonationAsync(requestId, userId, dto.Amount, dto.PaymentMethod, receiptUrl);
		if (request is null) return BadRequest("Could not fulfill donation. Ensure the request is still pending and is of type FinancialDonation.");

		// Notify patient
		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
		if (volunteer != null && request.Patient?.User != null)
		{
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				"تبرع جديد",
				$"قام المتطوع {volunteer.FullName} بالتبرع بمبلغ {dto.Amount} جنيه عبر {dto.PaymentMethod}."
			);
		}

		return Ok(new { message = "تم تسجيل التبرع بنجاح. شكراً لمساهمتك في مساعدة المرضى.", status = request.Status.ToString() });
	}

	[HttpGet("my-requests")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> GetMyRequests()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _volunteerRepository.GetMyRequestsAsync(userId);
		var result = _mapper.Map<IEnumerable<MyVolunteerRequestDto>>(requests);
		return Ok(result);
	}

	/// <summary>
	/// تأكيد تقديم الخدمة من قبل المتطوع
	/// </summary>
	[HttpPost("complete/{requestId}")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> CompleteRequest(int requestId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _volunteerRepository.CompleteRequestAsync(requestId, userId);

		if (request is null)
			return NotFound("Request not found, already completed, or you don't have access to it.");

		// إرسال إشعار للمريض
		if (request.Patient?.IdentityUserId != null)
		{
			var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				"تم تقديم الخدمة",
				$"قام المتطوع {volunteer?.FullName ?? "المتطوع"} بإكمال طلبك بنجاح. شكراً للمتطوعين."
			);
		}

		return Ok(new {
			message = "تم إكمال الطلب بنجاح",
			requestId = request.Id,
			status = request.Status.ToString()
		});
	}

	[HttpGet("handled-by-me")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetHandledByMe()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _volunteerRepository.GetRequestsHandledByVolunteerAsync(userId);
		var result = _mapper.Map<IEnumerable<VolunteerRequestSummaryDto>>(requests);
		return Ok(result);
	}
}
