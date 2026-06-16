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
public class MedicalRequestsController : ControllerBase
{
	private readonly IMedicalRequestRepository _medicalRequestRepository;
	private readonly IMapper _mapper;
	private readonly AppDbContext _context;
	private readonly IFileService _fileService;
	private readonly INotificationService _notificationService;

	public MedicalRequestsController(
		IMedicalRequestRepository medicalRequestRepository, 
		IMapper mapper, 
		AppDbContext context, 
		IFileService fileService,
		INotificationService notificationService)
	{
		_medicalRequestRepository = medicalRequestRepository;
		_mapper = mapper;
		_context = context;
		_fileService = fileService;
		_notificationService = notificationService;
	}

	/// <summary>
	/// طلبات الكشف/الاستشارة المطابقة للطبيب (نفس المحافظة + نفس التخصص).
	/// </summary>
	[HttpGet("for-doctor")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetForDoctor()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _medicalRequestRepository.GetRequestsForDoctorAsync(userId);
		var result = _mapper.Map<IEnumerable<MedicalRequestSummaryDto>>(requests);
		return Ok(result);
	}

	/// <summary>
	/// إنشاء طلب استشارة طبية جديد من قبل المريض.
	/// </summary>
	[HttpPost("create")]
	[Authorize(Roles = "Patient")]
	[RequestSizeLimit(20_000_000)] // 20MB max
	public async Task<IActionResult> CreateRequest([FromForm] CreateMedicalRequestDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		// البحث عن المريض
		var patient = await _context.Patients
			.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		
		if (patient is null) 
			return BadRequest("Patient profile not found. Please complete your profile first.");

		// التحقق من إكمال البروفايل
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user is null || !user.HasCompletedProfile)
			return BadRequest("Please complete your profile before creating a request.");

		// رفع الصور (إن وجدت)
		string? imagesPaths = null;
		if (dto.MedicalImages != null && dto.MedicalImages.Count > 0)
		{
			var uploadedPaths = new List<string>();
			foreach (var image in dto.MedicalImages)
			{
				var path = await _fileService.SaveImageAsync(image, "Uploads/MedicalImages");
				uploadedPaths.Add(path);
			}
			imagesPaths = string.Join(",", uploadedPaths);
		}

		var request = await _medicalRequestRepository.CreateRequestAsync(
			patient.Id, 
			dto.SpecialtyId, 
			dto.Description,
			imagesPaths,
			dto.ChronicDisease
		);

		return CreatedAtAction(nameof(CreateRequest), new { id = request.Id }, request);
	}

	/// <summary>
	/// جلب الملف الطبي الكامل لطلب محدد (للطبيب)
	/// </summary>
	[HttpGet("detail/{requestId}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetRequestDetail(int requestId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicalRequestRepository.GetRequestDetailAsync(requestId, userId);
		if (request is null) 
			return NotFound("Request not found or you don't have access to it.");

		var dto = _mapper.Map<MedicalRequestDetailDto>(request);
		return Ok(dto);
	}

	/// <summary>
	/// رد الدكتور على طلب المريض (قبول + تحديد ميعاد)
	/// </summary>
	[HttpPost("respond/{requestId}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> RespondToRequest(int requestId, [FromBody] RespondToRequestDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		// التحقق من صحة التاريخ
		if (dto.AppointmentDate.Date < DateTime.Now.Date)
			return BadRequest("Appointment date must be today or in the future.");

		var request = await _medicalRequestRepository.RespondToRequestAsync(
			requestId, 
			userId, 
			dto.AppointmentDate, 
			dto.DoctorNotes
		);

		if (request is null) 
			return NotFound("Request not found or you don't have access to it.");

		// إرسال إشعار للمريض
		var doctor = await _context.Doctors
			.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		
		if (doctor != null && request.Patient?.User != null)
		{
			var notificationTitle = "تم قبول طلبك الطبي";
			var notificationMessage = $"تم قبول طلب الاستشارة بواسطة د. {doctor.FullName}. موعد الكشف: {dto.AppointmentDate:dd/MM/yyyy hh:mm tt}";
			
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				notificationTitle,
				notificationMessage
			);
		}

		return Ok(new { 
			message = "Response sent successfully",
			requestId = request.Id,
			status = request.RequestStatus.ToString(),
			appointmentDate = request.AppointmentDate
		});
	}

	/// <summary>
	/// جلب طلبات المريض الشخصية مع تفاصيل الردود
	/// </summary>
	[HttpGet("my-requests")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> GetMyRequests()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _medicalRequestRepository.GetMyRequestsAsync(userId);
		var result = _mapper.Map<IEnumerable<MyMedicalRequestDto>>(requests);
		
		return Ok(result);
	}

	/// <summary>
	/// إلغاء طلب من قبل المريض
	/// </summary>
	[HttpPost("cancel/{requestId}")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> CancelRequest(int requestId, [FromBody] CancelRequestDto? dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicalRequestRepository.CancelRequestAsync(
			requestId, 
			userId, 
			dto?.CancellationReason
		);

		if (request is null) 
			return NotFound("Request not found or cannot be cancelled.");

		// إرسال إشعار للدكتور إذا كان الطلب مقبول
		if (request.Doctor != null && request.Doctor.IdentityUserId != null)
		{
			var notificationTitle = "تم إلغاء موعد";
			var notificationMessage = $"قام المريض بإلغاء موعد الاستشارة. السبب: {dto?.CancellationReason ?? "لم يتم ذكر السبب"}";
			
			await _notificationService.SendNotificationAsync(
				request.Doctor.IdentityUserId,
				notificationTitle,
				notificationMessage
			);
		}

		return Ok(new { 
			message = "Request cancelled successfully",
			requestId = request.Id,
			status = request.RequestStatus.ToString()
		});
	}

	/// <summary>
	/// إلغاء طلب من قبل الدكتور
	/// </summary>
	[HttpPost("cancel-by-doctor/{requestId}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CancelRequestByDoctor(int requestId, [FromBody] CancelRequestDto? dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicalRequestRepository.CancelRequestByDoctorAsync(
			requestId, 
			userId, 
			dto?.CancellationReason
		);

		if (request is null) 
			return NotFound("Request not found or you don't have access to it.");

		// إرسال إشعار للمريض
		if (request.Patient?.IdentityUserId != null)
		{
			var doctor = await _context.Doctors
				.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
			
			if (doctor != null)
			{
				var notificationTitle = "تم إلغاء الموعد";
				var notificationMessage = $"اعتذر د. {doctor.FullName} عن الموعد. السبب: {dto?.CancellationReason ?? "لم يتم ذكر السبب"}";
				
				await _notificationService.SendNotificationAsync(
					request.Patient.IdentityUserId,
					notificationTitle,
					notificationMessage
				);
			}
		}

		return Ok(new { 
			message = "Request cancelled successfully",
			requestId = request.Id,
			status = request.RequestStatus.ToString()
		});
	}

	/// <summary>
	/// جلب الطلبات التي تعامل معها الطبيب (مقبولة أو مكتملة)
	/// </summary>
	[HttpGet("handled-requests")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetHandledRequests()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var requests = await _medicalRequestRepository.GetRequestsHandledByDoctorAsync(userId);
		var result = _mapper.Map<IEnumerable<MedicalRequestSummaryDto>>(requests);
		return Ok(result);
	}

	[HttpGet("debug-for-doctor")]
	[AllowAnonymous]
	public async Task<IActionResult> DebugGetForDoctor([FromQuery] string doctorId)
	{
		var requests = await _medicalRequestRepository.GetRequestsForDoctorAsync(doctorId);
		var result = _mapper.Map<IEnumerable<MedicalRequestSummaryDto>>(requests);
		return Ok(new {
			count = requests.Count(),
			data = result,
			rawRequests = requests.Select(r => new { r.Id, r.PatientId, r.SpecialtyId, r.RequestStatus, PatientGovernorateId = r.Patient?.GovernorateId })
		});
	}

	/// <summary>
	/// تأكيد إتمام الاستشارة من قبل الطبيب
	/// </summary>
	[HttpPost("complete/{requestId}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CompleteRequest(int requestId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var request = await _medicalRequestRepository.CompleteRequestAsync(requestId, userId);

		if (request is null)
			return NotFound("Request not found, already completed, or you don't have access to it.");

		// إرسال إشعار للمريض
		if (request.Patient?.IdentityUserId != null)
		{
			var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				"تمت الاستشارة الطبية",
				$"تم إكمال استشارتك الطبية بواسطة د. {doctor?.FullName ?? "الطبيب"}. نتمنى لك الشفاء العاجل."
			);
		}

		return Ok(new {
			message = "تم إكمال الاستشارة بنجاح",
			requestId = request.Id,
			status = request.RequestStatus.ToString()
		});
	}
}


