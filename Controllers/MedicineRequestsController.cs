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
public class MedicineRequestsController : ControllerBase
{
	private readonly IMedicineRepository _medicineRepository;
	private readonly IFileService _fileService;
	private readonly IMapper _mapper;
	private readonly AppDbContext _context;
	private readonly INotificationService _notificationService;

	public MedicineRequestsController(
		IMedicineRepository medicineRepository,
		IFileService fileService,
		IMapper mapper,
		AppDbContext context,
		INotificationService notificationService)
	{
		_medicineRepository = medicineRepository;
		_fileService = fileService;
		_mapper = mapper;
		_context = context;
		_notificationService = notificationService;
	}

	/// <summary>
	/// إنشاء طلب دواء جديد مع صورة روشتة (multipart/form-data).
	/// </summary>
	[HttpPost]
	[Authorize(Roles = "Patient")]
	[RequestSizeLimit(10_000_000)]
	public async Task<IActionResult> Create([FromForm] CreateMedicineRequestDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var patient = await _context.Patients
			.Include(p => p.City)
			.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (patient is null) return BadRequest("Patient profile not found.");

		// منع المريض غير الموثق من إنشاء طلبات أدوية
		if (!patient.IsVerified)
		{
			return Forbid("Your account is not verified yet. Please wait for admin approval.");
		}

		string? imageUrl = null;
		if (dto.PrescriptionImage != null)
		{
			imageUrl = await _fileService.SaveImageAsync(dto.PrescriptionImage, "Uploads");
		}
		
		var request = await _medicineRepository.CreateRequestAsync(patient.Id, dto.MedicineName, imageUrl, dto.NeedDelivery);

		var mapped = _mapper.Map<MedicineRequestSummaryDto>(request);
		mapped.PrescriptionImageUrl = imageUrl;

		return Ok(mapped);
	}

	/// <summary>
		/// قائمة الطلبات المفتوحة للصيدليات في نفس المحافظة.
	/// </summary>
		[HttpGet("open")]
		[Authorize(Roles = "Pharmacist")]
		public async Task<IActionResult> GetOpen()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId is null) return Unauthorized();

			var requests = await _medicineRepository.GetOpenRequestsForPharmacyAsync(userId);
		var result = _mapper.Map<IEnumerable<MedicineRequestSummaryDto>>(requests);
		return Ok(result);
	}

	/// <summary>
	/// قبول طلب دواء من قبل الصيدلية
	/// </summary>
	[HttpPost("accept/{requestId}")]
	[Authorize(Roles = "Pharmacist")]
	public async Task<IActionResult> AcceptRequest(int requestId, [FromBody] RespondToMedicineRequestDto? dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicineRepository.AcceptRequestAsync(
			requestId, 
			userId, 
			dto?.PharmacyNotes
		);

		if (request is null) 
			return NotFound("Request not found or you don't have access to it.");

		// إرسال إشعار للمريض
		var pharmacy = await _context.Pharmacies
			.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		
		if (pharmacy != null && request.Patient?.User != null)
		{
			var notificationTitle = "بشرى سارة!";
			var notificationMessage = $"قامت صيدلية {pharmacy.PharmacyName} بتوفير دوائك. {(dto?.PharmacyNotes != null ? $"ملاحظات: {dto.PharmacyNotes}" : "")}";
			
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				notificationTitle,
				notificationMessage
			);
		}

		return Ok(new { 
			message = "Request accepted successfully",
			requestId = request.Id,
			status = request.RequestStatus.ToString()
		});
	}

	/// <summary>
	/// جلب طلبات المريض مع تفاصيل الصيدلية
	/// </summary>
	[HttpGet("my-requests")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> GetMyRequests()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _medicineRepository.GetMyRequestsAsync(userId);
		var result = _mapper.Map<IEnumerable<MyMedicineRequestDto>>(requests);
		
		return Ok(result);
	}

	/// <summary>
	/// جلب مهام التوصيل المتاحة للمتطوعين
	/// </summary>
	[HttpGet("delivery-tasks")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetDeliveryTasks()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _medicineRepository.GetDeliveryTasksAsync(userId);
		var result = _mapper.Map<IEnumerable<DeliveryTaskDto>>(requests);
		
		return Ok(result);
	}

	/// <summary>
	/// متطوع يأخذ مهمة توصيل
	/// </summary>
	[HttpPost("take-delivery/{requestId}")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> TakeDeliveryTask(int requestId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicineRepository.TakeDeliveryTaskAsync(requestId, userId);
		
		if (request is null) 
			return NotFound("Task not found or you don't have access to it.");

		return Ok(new { 
			message = "Delivery task taken successfully",
			requestId = request.Id,
			deliveryStatus = request.DeliveryStatus.ToString()
		});
	}

	/// <summary>
	/// تأكيد اكتمال التوصيل
	/// </summary>
	[HttpPost("mark-delivered/{requestId}")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> MarkAsDelivered(int requestId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicineRepository.MarkAsDeliveredAsync(requestId, userId);
		
		if (request is null) 
			return NotFound("Request not found or you don't have access to it.");

		// إرسال إشعار للمريض
		if (request.Patient?.User != null)
		{
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				"تم توصيل دوائك",
				"تم توصيل الدواء بنجاح من المتطوع. نتمنى لك الشفاء العاجل"
			);
		}

		return Ok(new { 
			message = "Delivery confirmed successfully",
			requestId = request.Id,
			deliveryStatus = request.DeliveryStatus.ToString()
		});
	}

	/// <summary>
	/// تأكيد صرف الدواء وإكمال الطلب من قبل الصيدلية
	/// </summary>
	[HttpPost("complete/{requestId}")]
	[Authorize(Roles = "Pharmacist")]
	public async Task<IActionResult> CompleteRequest(int requestId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicineRepository.CompleteRequestAsync(requestId, userId);

		if (request is null)
			return NotFound("Request not found, already completed, or you don't have access to it.");

		// إرسال إشعار للمريض
		if (request.Patient?.IdentityUserId != null)
		{
			var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				"تم صرف دوائك",
				$"قامت صيدلية {pharmacy?.PharmacyName ?? "الصيدلية"} بصرف دوائك بنجاح. نتمنى لك الشفاء العاجل."
			);
		}

		return Ok(new {
			message = "تم صرف الدواء وإكمال الطلب بنجاح",
			requestId = request.Id,
			status = request.RequestStatus.ToString()
		});
	}

	/// <summary>
	/// جلب المهام التي أخذها المتطوع حالياً (قيد التوصيل)
	/// </summary>
	[HttpGet("my-delivery-tasks")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetMyDeliveryTasks()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _medicineRepository.GetMyDeliveryTasksAsync(userId);
		var result = _mapper.Map<IEnumerable<DeliveryTaskDto>>(requests);
		
		return Ok(result);
	}

	/// <summary>
	/// جلب المهام التي أتمها المتطوع بنجاح (تم التوصيل)
	/// </summary>
	[HttpGet("completed-delivery-tasks")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetCompletedDeliveryTasks()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _medicineRepository.GetCompletedDeliveryTasksAsync(userId);
		var result = _mapper.Map<IEnumerable<DeliveryTaskDto>>(requests);
		
		return Ok(result);
	}
}
