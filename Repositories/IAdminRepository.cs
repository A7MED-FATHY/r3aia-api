using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;

namespace R3AIA.Repositories;

public interface IAdminRepository
{
	Task<IEnumerable<ApplicationUser>> GetPendingUsersAsync();
	Task<bool> VerifyUserAsync(UserVerificationDto dto);
	Task<bool> BanUserAsync(string userId);
	Task<bool> UnbanUserAsync(string userId);
	Task<DonationCase> CreateDonationCaseAsync(CreateDonationCaseDto dto, string imageUrl);
	Task<IEnumerable<UserReport>> GetAllReportsAsync();
	Task<bool> ResolveReportAsync(ResolveReportDto dto);

	Task<IEnumerable<AdminActiveRequestDto>> GetStalledRequestsAsync(TimeSpan threshold);
	Task<AdminRequestDetailDto?> GetRequestDetailAsync(string type, int id);
	Task<bool> DeleteDonationCaseAsync(int id);
	Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
	Task<bool> DeleteUserAsync(string userId);
	Task<IEnumerable<SupportMessage>> GetSupportMessagesAsync();
	Task<bool> ReplyToSupportMessageAsync(int messageId, string reply);
	Task<AdminCompletedStatsResponse> GetCompletedServicesAsync(int? governorateId);
}

public class AdminRepository : IAdminRepository
{
	private readonly AppDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ISupportRepository _supportRepository;

	public AdminRepository(AppDbContext context, UserManager<ApplicationUser> userManager, ISupportRepository supportRepository)
	{
		_context = context;
		_userManager = userManager;
		_supportRepository = supportRepository;
	}

	public async Task<IEnumerable<ApplicationUser>> GetPendingUsersAsync()
	{
		// أي مستخدم لم يتم توثيقه بعد وحالته معلقة
		return await _context.Users
			.Where(u => !u.IsVerified && u.AccountStatus == Enums.AccountStatus.Pending)
			.ToListAsync();
	}

	public async Task<bool> VerifyUserAsync(UserVerificationDto dto)
	{
		var user = await _userManager.FindByIdAsync(dto.UserId);
		if (user is null) return false;

		user.IsVerified = dto.IsApproved;
		user.AccountStatus = dto.IsApproved ? Enums.AccountStatus.Active : Enums.AccountStatus.Rejected;
		
		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded) return false;

		if (dto.IsApproved)
		{
			// Sync verification status to specific profile tables
			if (user.UserType == Enums.UserType.Patient)
			{
				var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);
				if (patient != null) patient.IsVerified = true;
			}
			else if (user.UserType == Enums.UserType.Doctor)
			{
				var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == user.Id);
				if (doctor != null) doctor.IsVerified = true;
			}
			else if (user.UserType == Enums.UserType.Pharmacist)
			{
				var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(ph => ph.IdentityUserId == user.Id);
				if (pharmacy != null) pharmacy.IsVerified = true;
			}
			else if (user.UserType == Enums.UserType.Volunteer)
			{
				var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == user.Id);
				if (volunteer != null) volunteer.IsVerified = true;
			}

			await _context.SaveChangesAsync();

			// Send notifications
			if (user.UserType == Enums.UserType.Patient)
			{
				await _supportRepository.PushNotificationAsync(user.Id,
					"تم توثيق حسابك بنجاح، يمكنك الآن البدء بطلب الأدوية.");
			}
			else
			{
				await _supportRepository.PushNotificationAsync(user.Id,
					"تم تفعيل حسابك، يمكنك الآن استقبال الطلبات.");
			}
		}

		return true;
	}

	public async Task<DonationCase> CreateDonationCaseAsync(CreateDonationCaseDto dto, string imageUrl)
	{
		var donationCase = new DonationCase
		{
			Title = dto.Title,
			Description = dto.Description,
			GoalAmount = dto.GoalAmount,
			CaseImage = imageUrl,
			PatientName = dto.PatientName,
			CreatedAt = DateTime.Now
		};

		_context.DonationCases.Add(donationCase);
		await _context.SaveChangesAsync();
		return donationCase;
	}

	public async Task<IEnumerable<UserReport>> GetAllReportsAsync()
	{
		return await _context.UserReports
			.Include(r => r.Reporter)
			.Include(r => r.ReportedUser)
			.ToListAsync();
	}

	public async Task<bool> ResolveReportAsync(ResolveReportDto dto)
	{
		var report = await _context.UserReports.FindAsync(dto.ReportId);
		if (report is null) return false;

		if (Enum.TryParse<ReportStatus>(dto.NewStatus, true, out var status))
			report.Status = status;
		else
			report.Status = ReportStatus.Resolved;

		report.AdminActionNotes = dto.AdminComment;

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> BanUserAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null) return false;

		user.AccountStatus = Enums.AccountStatus.Banned;
		user.IsActive = false;
		user.IsVerified = false;

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded) return false;

		await _supportRepository.PushNotificationAsync(user.Id,
			"تم حظر حسابك نهائياً بسبب مخالفة سياسات الاستخدام.");

		return true;
	}

	public async Task<bool> UnbanUserAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null) return false;

		// إعادة الحساب إلى حالة Pending ليعاد تقييمه ثم توثيقه
		user.AccountStatus = Enums.AccountStatus.Pending;
		user.IsActive = true;

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded) return false;

		await _supportRepository.PushNotificationAsync(user.Id,
			"تم فك الحظر عن حسابك، سيتم مراجعة بياناتك مرة أخرى.");

		return true;
	}

	public async Task<IEnumerable<AdminActiveRequestDto>> GetStalledRequestsAsync(TimeSpan threshold)
	{
		var now = DateTime.Now;

		var cutoffTime = now.Subtract(threshold);
		
		// Medical requests stalled (doctors bottleneck)
		var medicalRequests = await _context.MedicalRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Doctor) // Added Include for Doctor
			.Where(r => r.RequestStatus == Enums.RequestStatus.Pending &&
			            r.CreatedAt <= cutoffTime)
			.ToListAsync();

		var stalledMedical = medicalRequests.Select(r => new AdminActiveRequestDto
			{
				Id = r.Id,
				Type = "Medical",
				CreatedAt = r.CreatedAt,
				AgeMinutes = (now - r.CreatedAt).TotalMinutes,
				Bottleneck = "Doctor",
				PatientName = r.Patient.FullName,
				PatientGovernorate = r.Patient.Governorate?.Name ?? "غير محدد",
				PatientCity = r.Patient.City?.Name ?? "غير محدد",
				PatientPhone = r.Patient.PhoneNumber,
				TargetName = r.Doctor?.FullName ?? "",
				TargetPhone = r.Doctor?.PhoneNumber ?? "",
				Status = r.RequestStatus.ToString()
			}).ToList();

		// Medicine requests stalled (pharmacy or volunteer bottleneck)
		var stalledMedicineQuery = _context.MedicineRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Pharmacy) // Added Include for Pharmacy
			.Where(r => r.CreatedAt <= cutoffTime);

		var stalledMedicine = await stalledMedicineQuery
			.Select(r => new
			{
				Request = r,
				HasPharmacy = r.PharmacyId != null,
				HasDeliveryTask = _context.DeliveryTasks.Any(t =>
					t.MedicineRequestId == r.Id && t.TaskStatus != Enums.DeliveryStatus.Delivered)
			})
			.ToListAsync();

		var stalledMedicineDtos = stalledMedicine.Select(x =>
		{
			var bottleneck = !x.HasPharmacy ? "Pharmacy"
				: (x.Request.NeedDelivery && !x.HasDeliveryTask ? "Volunteer" : "Unknown");

			return new AdminActiveRequestDto
			{
				Id = x.Request.Id,
				Type = "Medicine",
				CreatedAt = x.Request.CreatedAt,
				AgeMinutes = (now - x.Request.CreatedAt).TotalMinutes,
				Bottleneck = bottleneck,
				PatientName = x.Request.Patient.FullName,
				PatientGovernorate = x.Request.Patient.Governorate?.Name ?? "غير محدد",
				PatientCity = x.Request.Patient.City?.Name ?? "غير محدد",
				PatientPhone = x.Request.Patient.PhoneNumber,
				TargetName = x.Request.Pharmacy?.PharmacyName ?? "",
				TargetPhone = x.Request.Pharmacy?.PhoneNumber ?? "",
				Status = x.Request.RequestStatus.ToString()
			};
		}).ToList();

		return stalledMedical.Concat(stalledMedicineDtos)
			.OrderByDescending(r => r.AgeMinutes)
			.ToList();
	}

	public async Task<AdminRequestDetailDto?> GetRequestDetailAsync(string type, int id)
	{
		var now = DateTime.Now;

		if (string.Equals(type, "Medical", StringComparison.OrdinalIgnoreCase))
		{
			var req = await _context.MedicalRequests
				.Include(r => r.Patient)
					.ThenInclude(p => p.City)
				.Include(r => r.Specialty)
				.Include(r => r.Doctor)
				.FirstOrDefaultAsync(r => r.Id == id);

			if (req is null) return null;

			var detail = new AdminRequestDetailDto
			{
				Id = req.Id,
				Type = "Medical",
				CreatedAt = req.CreatedAt,
				AgeMinutes = now.Subtract(req.CreatedAt).TotalMinutes,
				PatientName = req.Patient.FullName,
				PatientGovernorate = req.Patient.Governorate.Name,
				PatientCity = req.Patient.City.Name,
				PatientPhone = req.Patient.PhoneNumber,
				SpecialtyName = req.Specialty.Name,
				Description = req.Description
			};

			// إذا لم يتم قبول الطلب بعد، نعرض الأطباء المرشحين
			if (req.DoctorId == null)
			{
				var doctors = await _context.Doctors
					.Where(d => d.GovernorateId == req.Patient.GovernorateId &&
					            d.SpecialtyId == req.SpecialtyId)
					.ToListAsync();

				detail.SuggestedContacts = doctors
					.Select(d => new AdminContactSuggestionDto
					{
						Name = d.FullName,
						Role = "Doctor",
						PhoneNumber = d.PhoneNumber
					})
					.ToList();
			}

			return detail;
		}

		if (string.Equals(type, "Medicine", StringComparison.OrdinalIgnoreCase))
		{
			var req = await _context.MedicineRequests
				.Include(r => r.Patient)
					.ThenInclude(p => p.City)
				.Include(r => r.Pharmacy)
				.FirstOrDefaultAsync(r => r.Id == id);

			if (req is null) return null;

			var detail = new AdminRequestDetailDto
			{
				Id = req.Id,
				Type = "Medicine",
				CreatedAt = req.CreatedAt,
				AgeMinutes = now.Subtract(req.CreatedAt).TotalMinutes,
				PatientName = req.Patient.FullName,
				PatientGovernorate = req.Patient.Governorate.Name,
				PatientCity = req.Patient.City.Name,
				PatientPhone = req.Patient.PhoneNumber,
				PrescriptionImageUrl = req.PrescriptionImage
			};

			// إذا لم تُسند لصيدلية، نعرض الصيدليات المتاحة في نفس المحافظة
			if (req.PharmacyId == null)
			{
				var pharmacies = await _context.Pharmacies
					.Where(p => p.GovernorateId == req.Patient.GovernorateId)
					.ToListAsync();

				detail.SuggestedContacts = pharmacies
					.Select(p => new AdminContactSuggestionDto
					{
						Name = p.PharmacyName,
						Role = "Pharmacy",
						PhoneNumber = p.PhoneNumber
					})
					.ToList();
			}

			return detail;
		}

		return null;
	}

	public async Task<bool> DeleteDonationCaseAsync(int id)
	{
		var donationCase = await _context.DonationCases.FindAsync(id);
		if (donationCase is null) return false;

		_context.DonationCases.Remove(donationCase);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
	{
		return await _context.Users
			.OrderByDescending(u => u.CreatedAt)
			.ToListAsync();
	}

	public async Task<bool> DeleteUserAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null) return false;

		// We might need to delete related data (Patient, Doctor, etc.) if they have foreign keys that aren't cascading.
		// Looking at AppDbContext, we have Restrict on some relations.
		
		var result = await _userManager.DeleteAsync(user);
		return result.Succeeded;
	}

	public async Task<IEnumerable<SupportMessage>> GetSupportMessagesAsync()
	{
		return await _supportRepository.GetAllSupportMessagesAsync();
	}

	public async Task<bool> ReplyToSupportMessageAsync(int messageId, string reply)
	{
		return await _supportRepository.ReplyToSupportMessageAsync(messageId, reply);
	}

	public async Task<AdminCompletedStatsResponse> GetCompletedServicesAsync(int? governorateId)
	{
		var response = new AdminCompletedStatsResponse();

		// Base queries
		var medicalQuery = _context.MedicalRequests
			.Include(r => r.Patient).ThenInclude(p => p.Governorate)
			.Include(r => r.Doctor)
			.Include(r => r.Specialty)
			.Where(r => r.RequestStatus == Enums.RequestStatus.Completed);

		var medicineQuery = _context.MedicineRequests
			.Include(r => r.Patient).ThenInclude(p => p.Governorate)
			.Include(r => r.Pharmacy)
			.Where(r => r.RequestStatus == Enums.RequestStatus.Completed);

		var volunteerQuery = _context.VolunteerRequests
			.Include(r => r.Patient).ThenInclude(p => p.Governorate)
			.Include(r => r.Volunteer)
			.Where(r => r.Status == Enums.RequestStatus.Completed);

		// Apply governorate filter
		if (governorateId.HasValue)
		{
			medicalQuery = medicalQuery.Where(r => r.Patient.GovernorateId == governorateId.Value);
			medicineQuery = medicineQuery.Where(r => r.Patient.GovernorateId == governorateId.Value);
			volunteerQuery = volunteerQuery.Where(r => r.Patient.GovernorateId == governorateId.Value);
		}

		// Execute and map
		var medicals = await medicalQuery.ToListAsync();
		var medicines = await medicineQuery.ToListAsync();
		var volunteers = await volunteerQuery.ToListAsync();

		response.MedicalCompleted = medicals.Count;
		response.MedicineCompleted = medicines.Count;
		response.VolunteerCompleted = volunteers.Count;
		response.TotalCompleted = medicals.Count + medicines.Count + volunteers.Count;

		// Combine and map to DTOs
		foreach (var m in medicals)
		{
			response.Services.Add(new CompletedServiceDto
			{
				Id = m.Id,
				ServiceType = "Medical",
				PatientName = m.Patient.FullName,
				ProviderName = m.Doctor?.FullName ?? "غير محدد",
				GovernorateName = m.Patient.Governorate?.Name ?? "غير محدد",
				ServiceDate = m.CreatedAt,
				Description = m.Description,
				DoctorSpecialty = m.Specialty?.Name,
				ClinicAddress = m.Doctor?.ClinicAddress,
				DoctorNotes = m.DoctorNotes
			});
		}

		foreach (var m in medicines)
		{
			response.Services.Add(new CompletedServiceDto
			{
				Id = m.Id,
				ServiceType = "Medicine",
				PatientName = m.Patient.FullName,
				ProviderName = m.Pharmacy?.PharmacyName ?? "غير محدد",
				GovernorateName = m.Patient.Governorate?.Name ?? "غير محدد",
				ServiceDate = m.CreatedAt,
				Description = m.PharmacyNotes,
				MedicineName = m.MedicineName,
				PharmacyNotes = m.PharmacyNotes
			});
		}

		foreach (var v in volunteers)
		{
			response.Services.Add(new CompletedServiceDto
			{
				Id = v.Id,
				ServiceType = "Volunteer",
				PatientName = v.Patient.FullName,
				ProviderName = v.Volunteer?.FullName ?? "غير محدد",
				GovernorateName = v.Patient.Governorate?.Name ?? "غير محدد",
				ServiceDate = v.CreatedAt,
				Description = v.Description,
				VolunteerType = v.Type.ToString()
			});
		}

		// Order by date descending
		response.Services = response.Services.OrderByDescending(s => s.ServiceDate).ToList();

		return response;
	}
}
 

