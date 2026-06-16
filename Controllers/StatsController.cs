using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/Stats")]
[Authorize]
public class StatsController : ControllerBase
{
	private readonly AppDbContext _context;

	public StatsController(AppDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// إحصائيات عامة للموقع (علنية)
	/// </summary>
	[HttpGet("public")]
	[AllowAnonymous]
	public async Task<IActionResult> GetPublicStats()
	{
		var patientsCount = await _context.Patients.CountAsync();
		var volunteersCount = await _context.Volunteers.CountAsync();
		var doctorsCount = await _context.Doctors.CountAsync();
		var totalCollected = await _context.PatientDonations
			.Where(d => d.Status == DonationStatus.Approved)
			.SumAsync(d => (decimal?)d.Amount) ?? 0;

		return Ok(new
		{
			patientsCount,
			volunteersCount,
			doctorsCount,
			totalCollected = (long)totalCollected,
			activeCases = await _context.PatientCases.CountAsync(c => c.Status == CaseStatus.Approved && c.CollectedAmount < c.RequiredAmount)
		});
	}

	/// <summary>
	/// إحصائيات داشبورد المريض
	/// </summary>
	[HttpGet("patient")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> GetPatientStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (patient is null) return Ok(new { totalRequests = 0, pendingRequests = 0, acceptedRequests = 0, medicineRequests = 0 });

		var medicalRequests = await _context.MedicalRequests
			.Where(r => r.PatientId == patient.Id)
			.ToListAsync();

		var medicineRequests = await _context.MedicineRequests
			.Where(r => r.PatientId == patient.Id)
			.ToListAsync();

		return Ok(new
		{
			totalRequests = medicalRequests.Count + medicineRequests.Count,
			pendingRequests = medicalRequests.Count(r => r.RequestStatus == RequestStatus.Pending)
						   + medicineRequests.Count(r => r.RequestStatus == RequestStatus.Pending),
			acceptedRequests = medicalRequests.Count(r => r.RequestStatus == RequestStatus.Accepted)
						    + medicineRequests.Count(r => r.RequestStatus == RequestStatus.Accepted),
			medicineRequests = medicineRequests.Count
		});
	}

	/// <summary>
	/// إحصائيات داشبورد الدكتور
	/// </summary>
	[HttpGet("doctor")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetDoctorStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor is null) return Ok(new { availableConsultations = 0, acceptedConsultations = 0, completedConsultations = 0 });

		// الطلبات المتاحة = نفس المحافظة + نفس التخصص + Pending
		var availableCount = await _context.MedicalRequests
			.Include(r => r.Patient)
			.CountAsync(r => r.RequestStatus == RequestStatus.Pending
						  && r.SpecialtyId == doctor.SpecialtyId
						  && r.Patient.GovernorateId == doctor.GovernorateId);

		var acceptedCount = await _context.MedicalRequests
			.CountAsync(r => r.DoctorId == doctor.Id && r.RequestStatus == RequestStatus.Accepted);

		var completedCount = await _context.MedicalRequests
			.CountAsync(r => r.DoctorId == doctor.Id && r.RequestStatus == RequestStatus.Completed);

		return Ok(new
		{
			availableConsultations = availableCount,
			acceptedConsultations = acceptedCount,
			completedConsultations = completedCount
		});
	}

	/// <summary>
	/// إحصائيات داشبورد الصيدلية
	/// </summary>
	[HttpGet("pharmacy")]
	[Authorize(Roles = "Pharmacist")]
	public async Task<IActionResult> GetPharmacyStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (pharmacy is null) return Ok(new { availableRequests = 0, fulfilledRequests = 0, totalRequests = 0 });

		// الطلبات المفتوحة = نفس المحافظة + Pending
		var availableCount = await _context.MedicineRequests
			.Include(r => r.Patient)
			.CountAsync(r => r.RequestStatus == RequestStatus.Pending
						  && r.Patient.GovernorateId == pharmacy.GovernorateId);

		var acceptedCount = await _context.MedicineRequests
			.CountAsync(r => r.PharmacyId == pharmacy.Id
						  && (r.RequestStatus == RequestStatus.Accepted || r.RequestStatus == RequestStatus.Completed));

		var totalCount = await _context.MedicineRequests
			.CountAsync(r => r.PharmacyId == pharmacy.Id);

		return Ok(new
		{
			availableRequests = availableCount,
			acceptedRequests = acceptedCount,
			totalRequests = totalCount
		});
	}

	/// <summary>
	/// إحصائيات داشبورد المتطوع
	/// </summary>
	[HttpGet("volunteer")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetVolunteerStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
		if (volunteer is null) return Ok(new { availableTasks = 0, myTasks = 0, completedTasks = 0 });

		// الطلبات المتاحة = الطلبات التي تحتاج توصيل وحالتها Accepted ولم تُؤخذ بعد من أي متطوع
		var takenRequestIds = await _context.DeliveryTasks.Select(dt => dt.MedicineRequestId).ToListAsync();
		var availableCount = await _context.MedicineRequests
			.CountAsync(r => r.NeedDelivery 
						  && r.RequestStatus == RequestStatus.Accepted 
						  && !takenRequestIds.Contains(r.Id)
						  && r.Patient.GovernorateId == volunteer.GovernorateId);

		var myTasksCount = await _context.DeliveryTasks
			.CountAsync(t => t.VolunteerId == volunteer.Id && t.TaskStatus == DeliveryStatus.Taken)
			+ await _context.VolunteerRequests
			.CountAsync(r => r.VolunteerId == volunteer.Id && r.Status == RequestStatus.Accepted);

		var completedCount = await _context.DeliveryTasks
			.CountAsync(t => t.VolunteerId == volunteer.Id && t.TaskStatus == DeliveryStatus.Delivered);

		return Ok(new
		{
			availableTasks = availableCount,
			myTasks = myTasksCount,
			completedTasks = completedCount
		});
	}

	/// <summary>
	/// إحصائيات شاملة للأدمن (Dashboard)
	/// </summary>
	[HttpGet("total")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> GetTotalStats()
	{
		var patients = await _context.Patients.CountAsync();
		var doctors = await _context.Doctors.CountAsync();
		var volunteers = await _context.Volunteers.CountAsync();
		var pharmacies = await _context.Pharmacies.CountAsync();

		var medicalRequests = await _context.MedicalRequests.CountAsync();
		var medicineRequests = await _context.MedicineRequests.CountAsync();

		var totalDonations = await _context.PatientDonations
			.Where(d => d.Status == DonationStatus.Approved)
			.SumAsync(d => (decimal?)d.Amount) ?? 0;

		var activeCases = await _context.PatientCases
			.CountAsync(c => c.Status == CaseStatus.Approved && c.CollectedAmount < c.RequiredAmount);

		var pendingVerifications = await _context.Users.CountAsync(u => u.UserType != UserType.Admin && !u.IsVerified && u.HasCompletedProfile);

		return Ok(new
		{
			patients,
			doctors,
			volunteers,
			pharmacies,
			totalUsers = patients + doctors + volunteers + pharmacies,
			pendingVerifications,
			totalMedicalRequests = medicalRequests,
			totalMedicineRequests = medicineRequests,
			totalDonations = (long)totalDonations,
			activeDonationCases = activeCases,
			activeCases
		});
	}

	/// <summary>
	/// مسار زمني للتبرعات المكتملة
	/// </summary>
	[HttpGet("donations-timeline")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> GetDonationsTimeline()
	{
		var donations = await _context.PatientDonations
			.Where(d => d.Status == DonationStatus.Approved)
			.OrderBy(d => d.CreatedAt)
			.Select(d => new { d.Amount, d.CreatedAt })
			.ToListAsync();

		return Ok(donations);
	}

	/// <summary>
	/// بيانات رعاية بوكس العلنية (للعرض في التطبيق بدون تسجيل دخول)
	/// </summary>
	[HttpGet("r3aia-box")]
	[AllowAnonymous]
	public async Task<IActionResult> GetR3aiaBoxPublicInfo()
	{
		var setting = await _context.R3aiaBoxSettings
			.Include(s => s.Images.OrderBy(i => i.DisplayOrder))
			.FirstOrDefaultAsync();

		if (setting == null || !setting.IsActive)
			return Ok(new { isActive = false });

		return Ok(new
		{
			isActive = true,
			price = setting.Price,
			availableQuantity = setting.AvailableQuantity,
			shortDescription = setting.ShortDescription,
			detailedDescription = setting.DetailedDescription,
			appDownloadLink = setting.AppDownloadLink,
			images = setting.Images.Select(i => new
			{
				id = i.Id,
				imageUrl = i.ImageUrl,
				displayOrder = i.DisplayOrder
			}).ToList()
		});
	}
}
