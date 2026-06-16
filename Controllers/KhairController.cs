using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;
using Microsoft.Extensions.DependencyInjection;
using static R3AIA.Models.Enums;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/Khair")]
public class KhairController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly R3AIA.Services.IFileService _fileService;
	private readonly INotificationService _notificationService;

	public KhairController(AppDbContext context, R3AIA.Services.IFileService fileService, INotificationService notificationService)
	{
		_context = context;
		_fileService = fileService;
		_notificationService = notificationService;
	}

	// ─────────────────────────────────────────────────────────────
	// BADGE HELPER
	// ─────────────────────────────────────────────────────────────
	private static string GetBadgeLevel(int totalFree) => totalFree switch
	{
		>= 50 => "gold",
		>= 20 => "silver",
		>= 5  => "bronze",
		_      => "none"
	};

	// ─────────────────────────────────────────────────────────────
	// GET: /api/Khair/doctors
	// عرض قائمة أطباء الخير (للجميع بدون تسجيل)
	// ─────────────────────────────────────────────────────────────
	[HttpGet("doctors")]
	[AllowAnonymous]
	public async Task<IActionResult> GetDoctors(
		[FromQuery] string? filter = null,      // "free" | "discounted" | "top_rated"
		[FromQuery] int? governorateId = null,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20)
	{
		var query = _context.KhairDoctors
			.Include(kd => kd.Doctor)
				.ThenInclude(d => d.Specialty)
			.Include(kd => kd.Doctor)
				.ThenInclude(d => d.Governorate)
			.Include(kd => kd.Slots)
			.Where(kd => kd.IsActive && (kd.Doctor.IsVerified || kd.Doctor.User.IsVerified));

		// Filters
		var filterValue = filter?.ToLower();
		if (filterValue == "free")
			query = query.Where(kd => kd.ConsultationType == ConsultationType.Free);
		else if (filterValue == "discounted")
			query = query.Where(kd => kd.ConsultationType == ConsultationType.Discounted);

		if (governorateId.HasValue)
			query = query.Where(kd => kd.Doctor.GovernorateId == governorateId.Value);

		if (filter == "top_rated")
			query = query.OrderByDescending(kd => kd.Rating);
		else
			query = query.OrderByDescending(kd => kd.TotalFreeConsultations)
						 .ThenByDescending(kd => kd.Rating);

		var total = await query.CountAsync();
		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(kd => new KhairDoctorListDto
			{
				KhairDoctorId         = kd.Id,
				DoctorId              = kd.DoctorId,
				FullName              = kd.Doctor.FullName,
				SpecialtyName         = kd.Doctor.Specialty.Name,
				GovernorateName       = kd.Doctor.Governorate.Name,
				BioNotes              = kd.BioNotes,
				ConsultationType      = kd.ConsultationType,
				DiscountedPrice       = kd.DiscountedPrice,
				Rating                = kd.Rating,
				RatingCount           = kd.RatingCount,
				FreeDailyLimit        = kd.FreeDailyLimit,
				TotalFreeConsultations = kd.TotalFreeConsultations,
				RegularPrice          = kd.RegularPrice,
				ProfilePictureUrl     = kd.Doctor.ProfilePictureUrl,
				BadgeLevel            = GetBadgeLevel(kd.TotalFreeConsultations)
			})
			.ToListAsync();

		return Ok(new { total, page, pageSize, items });
	}

	// ─────────────────────────────────────────────────────────────
	// GET: /api/Khair/doctors/{id}
	// تفاصيل طبيب بعينه
	// ─────────────────────────────────────────────────────────────
	[HttpGet("doctors/{id:int}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetDoctorDetail(int id)
	{
		var kd = await _context.KhairDoctors
			.Include(x => x.Doctor).ThenInclude(d => d.Specialty)
			.Include(x => x.Doctor).ThenInclude(d => d.Governorate)
			.Include(x => x.Slots)
			.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

		if (kd == null) return NotFound(new { message = "الطبيب غير موجود" });

		var availableSlots = kd.Slots.Count(s => !s.IsBooked && s.SlotDate >= DateOnly.FromDateTime(DateTime.Today));

		return Ok(new KhairDoctorDetailDto
		{
			KhairDoctorId          = kd.Id,
			DoctorId               = kd.DoctorId,
			FullName               = kd.Doctor.FullName,
			SpecialtyName          = kd.Doctor.Specialty.Name,
			GovernorateName        = kd.Doctor.Governorate.Name,
			ClinicAddress          = kd.Doctor.ClinicAddress,
			BioNotes               = kd.BioNotes,
			ConsultationType       = kd.ConsultationType,
			DiscountedPrice        = kd.DiscountedPrice,
			Rating                 = kd.Rating,
			RatingCount            = kd.RatingCount,
			FreeDailyLimit         = kd.FreeDailyLimit,
			TotalFreeConsultations = kd.TotalFreeConsultations,
			RegularPrice           = kd.RegularPrice,
			ProfilePictureUrl      = kd.Doctor.ProfilePictureUrl,
			BadgeLevel             = GetBadgeLevel(kd.TotalFreeConsultations),
			AvailableSlotsCount    = kd.Slots.Count(s => !s.IsBooked && s.SlotDate >= DateOnly.FromDateTime(DateTime.Today))
		});
	}

	// ─────────────────────────────────────────────────────────────
	// GET: /api/Khair/doctors/{id}/slots
	// المواعيد المتاحة لطبيب
	// ─────────────────────────────────────────────────────────────
	[HttpGet("doctors/{id:int}/slots")]
	[AllowAnonymous]
	public async Task<IActionResult> GetSlots(int id)
	{
		var slots = await _context.KhairAppointmentSlots
			.Where(s => s.KhairDoctorId == id
					 && !s.IsBooked
					 && s.SlotDate >= DateOnly.FromDateTime(DateTime.Today))
			.OrderBy(s => s.SlotDate).ThenBy(s => s.StartTime)
			.Select(s => new KhairSlotDto
			{
				SlotId    = s.Id,
				SlotDate  = s.SlotDate,
				StartTime = s.StartTime,
				EndTime   = s.EndTime,
				IsBooked  = s.IsBooked
			})
			.ToListAsync();

		return Ok(slots);
	}

	// ─────────────────────────────────────────────────────────────
	// POST: /api/Khair/book
	// حجز موعد (يتطلب تسجيل دخول كـ Patient أو Premium)
	// ─────────────────────────────────────────────────────────────
	[HttpPost("book")]
	[Authorize(Roles = "Patient,Premium")]
	public async Task<IActionResult> BookAppointment([FromBody] KhairBookRequest dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId == null) return Unauthorized();

		var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		// إذا كان المستخدم Premium وليس لديه سجل مريض، ننشئ له واحد تلقائياً
		if (patient == null)
		{
			var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (appUser == null) return Unauthorized();

			if (appUser.UserType == UserType.Premium)
			{
				// إنشاء سجل مريض تلقائي لمستخدم Premium
				var defaultGov = await _context.Governorates.FirstAsync();
				var defaultCity = await _context.Cities.FirstAsync(c => c.GovernorateId == defaultGov.Id);
				patient = new Patient
				{
					IdentityUserId = appUser.Id,
					FullName = appUser.FullName,
					NationalID = appUser.NationalID ?? "0000000000000",
					PhoneNumber = appUser.PhoneNumber ?? "N/A",
					GovernorateId = defaultGov.Id,
					CityId = defaultCity.Id,
					Address = "Premium User",
					NIDImage = "N/A",
					SocialProofImage = "N/A",
					HasChronicDisease = false,
					IsVerified = true
				};
				_context.Patients.Add(patient);
				await _context.SaveChangesAsync();
			}
			else
			{
				return BadRequest(new { message = "الملف الشخصي للمريض غير مكتمل" });
			}
		}

		var slot = await _context.KhairAppointmentSlots
			.Include(s => s.KhairDoctor)
			.FirstOrDefaultAsync(s => s.Id == dto.SlotId);

		if (slot == null) return NotFound(new { message = "الموعد غير موجود" });
		if (slot.IsBooked) return BadRequest(new { message = "هذا الموعد محجوز بالفعل" });
		if (slot.SlotDate < DateOnly.FromDateTime(DateTime.Today))
			return BadRequest(new { message = "انتهى هذا الموعد" });

		// منع حجز نفس الطبيب أكثر من مرة في نفس اليوم
		var existingBooking = await _context.KhairBookings
			.Include(b => b.Slot)
			.AnyAsync(b => b.PatientId == patient.Id
					    && b.KhairDoctorId == slot.KhairDoctorId
					    && b.Slot.SlotDate == slot.SlotDate
					    && b.Status != KhairBookingStatus.Cancelled);

		if (existingBooking)
			return BadRequest(new { message = "لديك حجز مسبق عند هذا الطبيب في نفس اليوم" });

		// التحقق من الحد اليومي للحالات المجانية
		if (slot.KhairDoctor.ConsultationType == ConsultationType.Free)
		{
			var todayFreeBookings = await _context.KhairBookings
				.Include(b => b.Slot)
				.CountAsync(b => b.KhairDoctorId == slot.KhairDoctorId
							  && b.Slot.SlotDate == slot.SlotDate
							  && b.Status != KhairBookingStatus.Cancelled);

			if (todayFreeBookings >= slot.KhairDoctor.FreeDailyLimit)
				return BadRequest(new { message = "وصل الطبيب للحد الأقصى من الحالات المجانية اليوم" });
		}

		// إنشاء الحجز
		slot.IsBooked = true;
		var booking = new KhairBooking
		{
			PatientId      = patient.Id,
			KhairDoctorId  = slot.KhairDoctorId,
			SlotId         = slot.Id,
			PatientNotes   = dto.PatientNotes,
			Status         = KhairBookingStatus.Confirmed
		};

		_context.KhairBookings.Add(booking);
		await _context.SaveChangesAsync();

		// إرسال إشعار للطبيب
		try
		{
			var doctorUser = await _context.KhairDoctors
				.Include(kd => kd.Doctor)
				.Where(kd => kd.Id == slot.KhairDoctorId)
				.Select(kd => kd.Doctor.IdentityUserId)
				.FirstOrDefaultAsync();

			if (doctorUser != null)
			{
				await _notificationService.SendNotificationAsync(
					doctorUser, 
					"حجز جديد 🗓️", 
					$"لديك حجز جديد من المريض {patient.FullName} ليوم {slot.SlotDate:yyyy-MM-dd} الساعة {slot.StartTime:hh:mm tt}");
			}
		}
		catch (Exception ex)
		{
			// لا نريد فشل الحجز بسبب فشل الإشعار، فقط نسجل الخطأ
			Console.WriteLine($"Notification Error: {ex.Message}");
		}

		return Ok(new { message = "تم تأكيد الحجز بنجاح", bookingId = booking.Id });
	}

	// ─────────────────────────────────────────────────────────────
	// GET: /api/Khair/my-bookings
	// حجوزاتي كمريض أو Premium
	// ─────────────────────────────────────────────────────────────
	[HttpGet("my-bookings")]
	[Authorize(Roles = "Patient,Premium")]
	public async Task<IActionResult> GetMyBookings()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (patient == null) return Ok(new List<object>());

		var bookings = await _context.KhairBookings
			.Include(b => b.KhairDoctor).ThenInclude(kd => kd.Doctor).ThenInclude(d => d.Specialty)
			.Include(b => b.KhairDoctor).ThenInclude(kd => kd.Doctor).ThenInclude(d => d.Governorate)
			.Include(b => b.Slot)
			.Where(b => b.PatientId == patient.Id)
			.OrderByDescending(b => b.CreatedAt)
			.Select(b => new KhairBookingDto
			{
				BookingId        = b.Id,
				DoctorName       = b.KhairDoctor.Doctor.FullName,
				SpecialtyName    = b.KhairDoctor.Doctor.Specialty.Name,
				GovernorateName  = b.KhairDoctor.Doctor.Governorate.Name,
				ConsultationType = b.KhairDoctor.ConsultationType,
				Price            = b.KhairDoctor.DiscountedPrice,
				SlotDate         = b.Slot.SlotDate,
				StartTime        = b.Slot.StartTime,
				EndTime          = b.Slot.EndTime,
				Status           = b.Status,
				CreatedAt        = b.CreatedAt
			})
			.ToListAsync();

		return Ok(bookings);
	}

	// ─────────────────────────────────────────────────────────────
	// POST: /api/Khair/cancel-booking/{id}
	// إلغاء حجز
	// ─────────────────────────────────────────────────────────────
	[HttpPost("cancel-booking/{id:int}")]
	[Authorize(Roles = "Patient,Premium")]
	public async Task<IActionResult> CancelBooking(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (patient == null) return Unauthorized();

		var booking = await _context.KhairBookings
			.Include(b => b.Slot)
			.FirstOrDefaultAsync(b => b.Id == id && b.PatientId == patient.Id);

		if (booking == null) return NotFound(new { message = "الحجز غير موجود" });
		if (booking.Status == KhairBookingStatus.Cancelled)
			return BadRequest(new { message = "الحجز ملغي بالفعل" });

		booking.Status = KhairBookingStatus.Cancelled;
		booking.Slot.IsBooked = false;
		await _context.SaveChangesAsync();

		return Ok(new { message = "تم إلغاء الحجز" });
	}

	// ─────────────────────────────────────────────────────────────
	// GET: /api/Khair/doctor-bookings
	// حجوزات الطبيب (للطبيب نفسه)
	// ─────────────────────────────────────────────────────────────
	[HttpGet("doctor-bookings")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetDoctorBookings()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor == null) return NotFound(new { message = "الطبيب غير موجود" });

		var khair = await _context.KhairDoctors.FirstOrDefaultAsync(kd => kd.DoctorId == doctor.Id);
		if (khair == null) return Ok(new List<object>()); // Not registered in Khair

		var bookings = await _context.KhairBookings
			.Include(b => b.Patient)
			.Include(b => b.Slot)
			.Where(b => b.KhairDoctorId == khair.Id)
			.OrderByDescending(b => b.Slot.SlotDate).ThenByDescending(b => b.Slot.StartTime)
			.Select(b => new
			{
				BookingId = b.Id,
				PatientName = b.Patient.FullName,
				PatientPhone = b.Patient.PhoneNumber,
				PatientNotes = b.PatientNotes,
				SlotDate = b.Slot.SlotDate,
				StartTime = b.Slot.StartTime,
				EndTime = b.Slot.EndTime,
				Status = b.Status,
				CreatedAt = b.CreatedAt
			})
			.ToListAsync();

		return Ok(bookings);
	}

	// ─────────────────────────────────────────────────────────────
	// PUT: /api/Khair/update-booking-status/{id}
	// تحديث حالة الحجز من قبل الطبيب
	// ─────────────────────────────────────────────────────────────
	[HttpPut("update-booking-status/{id:int}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] KhairUpdateStatusDto dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor == null) return Unauthorized();

		var khair = await _context.KhairDoctors.FirstOrDefaultAsync(kd => kd.DoctorId == doctor.Id);
		if (khair == null) return NotFound();

		var booking = await _context.KhairBookings
			.Include(b => b.Slot)
			.FirstOrDefaultAsync(b => b.Id == id && b.KhairDoctorId == khair.Id);

		if (booking == null) return NotFound(new { message = "الحجز غير موجود" });

		booking.Status = dto.Status;

		if (dto.Status == KhairBookingStatus.Cancelled)
		{
			booking.Slot.IsBooked = false; // Free up the slot if cancelled
		}

		await _context.SaveChangesAsync();
		return Ok(new { message = "تم التحديث بنجاح" });
	}

	// ─────────────────────────────────────────────────────────────
	// GET: /api/Khair/doctor-setup
	// جلب إعداد الطبيب الحالي
	// ─────────────────────────────────────────────────────────────
	[HttpGet("doctor-setup")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetDoctorSetup()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor == null) return NotFound(new { message = "الطبيب غير موجود" });

		var khair = await _context.KhairDoctors
			.FirstOrDefaultAsync(kd => kd.DoctorId == doctor.Id);

		if (khair == null)
			return Ok(new { isRegistered = false });

		return Ok(new
		{
			isRegistered      = true,
			consultationType  = khair.ConsultationType.ToString(),
			discountedPrice   = khair.DiscountedPrice,
			regularPrice      = khair.RegularPrice,
			freeDailyLimit    = khair.FreeDailyLimit,
			bioNotes          = khair.BioNotes,
			rating            = khair.Rating,
			totalFree         = khair.TotalFreeConsultations,
			badgeLevel        = GetBadgeLevel(khair.TotalFreeConsultations),
			isActive          = khair.IsActive,
			khairDoctorId     = khair.Id
		});
	}

	// ─────────────────────────────────────────────────────────────
	// POST: /api/Khair/doctor-setup
	// إنشاء أو تحديث إعداد الطبيب
	// ─────────────────────────────────────────────────────────────
	[HttpPost("doctor-setup")]
	[Authorize(Roles = "Doctor")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> SaveDoctorSetup([FromForm] KhairSetupRequest dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor == null) return NotFound(new { message = "الطبيب غير موجود" });

		if (dto.ConsultationType == ConsultationType.Discounted && (dto.DiscountedPrice == null || dto.DiscountedPrice <= 0))
			return BadRequest(new { message = "يجب تحديد سعر الكشف المخفض" });

		var khair = await _context.KhairDoctors.FirstOrDefaultAsync(kd => kd.DoctorId == doctor.Id);

		if (khair == null)
		{
			// إنشاء جديد
			khair = new KhairDoctor
			{
				DoctorId         = doctor.Id,
				ConsultationType = dto.ConsultationType,
				DiscountedPrice  = dto.ConsultationType == ConsultationType.Discounted ? dto.DiscountedPrice : null,
				RegularPrice     = dto.ConsultationType == ConsultationType.Discounted ? dto.RegularPrice : null,
				FreeDailyLimit   = dto.ConsultationType == ConsultationType.Free ? dto.FreeDailyLimit : 0,
				BioNotes         = dto.BioNotes,
				IsActive         = true
			};
			_context.KhairDoctors.Add(khair);
		}
		else
		{
			// تحديث
			khair.ConsultationType = dto.ConsultationType;
			khair.DiscountedPrice  = dto.ConsultationType == ConsultationType.Discounted ? dto.DiscountedPrice : null;
			khair.RegularPrice     = dto.ConsultationType == ConsultationType.Discounted ? dto.RegularPrice : null;
			khair.FreeDailyLimit   = dto.ConsultationType == ConsultationType.Free ? dto.FreeDailyLimit : 0;
			khair.BioNotes         = dto.BioNotes;
			khair.IsActive         = true;
		}

		// معالجة الصورة الشخصية إذا تم رفعها
		if (dto.ProfilePicture != null)
		{
			var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
			doctor.ProfilePictureUrl = await fileService.SaveImageAsync(dto.ProfilePicture, "doctors");
		}

		await _context.SaveChangesAsync();
		return Ok(new { message = "تم حفظ الإعدادات بنجاح", khairDoctorId = khair.Id });
	}

	// ─────────────────────────────────────────────────────────────
	// POST: /api/Khair/add-slot
	// إضافة موعد جديد (للطبيب)
	// ─────────────────────────────────────────────────────────────
	[HttpPost("add-slot")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> AddSlot([FromBody] KhairAddSlotRequest dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor == null) return NotFound(new { message = "الطبيب غير موجود" });

		var khair = await _context.KhairDoctors.FirstOrDefaultAsync(kd => kd.DoctorId == doctor.Id);
		if (khair == null) return BadRequest(new { message = "يجب إتمام إعداد طبيب الخير أولاً" });

		if (dto.SlotDate < DateOnly.FromDateTime(DateTime.Today))
			return BadRequest(new { message = "لا يمكن إضافة موعد في الماضي" });

		if (dto.EndTime <= dto.StartTime)
			return BadRequest(new { message = "وقت النهاية يجب أن يكون بعد وقت البداية" });

		// التحقق من عدم التداخل مع مواعيد أخرى
		var conflict = await _context.KhairAppointmentSlots
			.AnyAsync(s => s.KhairDoctorId == khair.Id
						&& s.SlotDate == dto.SlotDate
						&& s.StartTime < dto.EndTime
						&& s.EndTime > dto.StartTime);

		if (conflict) return BadRequest(new { message = "يوجد تعارض مع موعد آخر في هذا الوقت" });

		var slot = new KhairAppointmentSlot
		{
			KhairDoctorId = khair.Id,
			SlotDate      = dto.SlotDate,
			StartTime     = dto.StartTime,
			EndTime       = dto.EndTime
		};
		_context.KhairAppointmentSlots.Add(slot);
		await _context.SaveChangesAsync();

		return Ok(new { message = "تم إضافة الموعد بنجاح", slotId = slot.Id });
	}

	// ─────────────────────────────────────────────────────────────
	// GET: /api/Khair/my-slots
	// مواعيد الطبيب الحالي
	// ─────────────────────────────────────────────────────────────
	[HttpGet("my-slots")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetMySlots()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor == null) return NotFound();

		var khair = await _context.KhairDoctors.FirstOrDefaultAsync(kd => kd.DoctorId == doctor.Id);
		if (khair == null) return Ok(new List<object>());

		var slots = await _context.KhairAppointmentSlots
			.Where(s => s.KhairDoctorId == khair.Id && s.SlotDate >= DateOnly.FromDateTime(DateTime.Today))
			.OrderBy(s => s.SlotDate).ThenBy(s => s.StartTime)
			.Select(s => new KhairSlotDto
			{
				SlotId    = s.Id,
				SlotDate  = s.SlotDate,
				StartTime = s.StartTime,
				EndTime   = s.EndTime,
				IsBooked  = s.IsBooked
			})
			.ToListAsync();

		return Ok(slots);
	}

	// ─────────────────────────────────────────────────────────────
	// DELETE: /api/Khair/slot/{id}
	// حذف موعد (للطبيب)
	// ─────────────────────────────────────────────────────────────
	[HttpDelete("slot/{id:int}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> DeleteSlot(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor == null) return Unauthorized();

		var khair = await _context.KhairDoctors.FirstOrDefaultAsync(kd => kd.DoctorId == doctor.Id);
		if (khair == null) return NotFound();

		var slot = await _context.KhairAppointmentSlots
			.FirstOrDefaultAsync(s => s.Id == id && s.KhairDoctorId == khair.Id);

		if (slot == null) return NotFound(new { message = "الموعد غير موجود" });
		if (slot.IsBooked) return BadRequest(new { message = "لا يمكن حذف موعد محجوز" });

		_context.KhairAppointmentSlots.Remove(slot);
		await _context.SaveChangesAsync();

		return Ok(new { message = "تم حذف الموعد" });
	}
}
