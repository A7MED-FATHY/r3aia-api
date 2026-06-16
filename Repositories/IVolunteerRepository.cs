using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Repositories;

public interface IVolunteerRepository
{
	Task<VolunteerRequest> CreateRequestAsync(int patientId, VolunteerRequestType type, string description);
	Task<IEnumerable<VolunteerRequest>> GetAvailableRequestsForVolunteerAsync(string volunteerUserId);
	Task<VolunteerRequest?> AcceptRequestAsync(int requestId, string volunteerUserId);
	Task<VolunteerRequest?> FulfillFinancialDonationAsync(int requestId, string volunteerUserId, decimal amount, PaymentMethod paymentMethod, string? receiptUrl);
	Task<IEnumerable<VolunteerRequest>> GetMyRequestsAsync(string patientUserId);

	/// <summary>
	/// إكمال الطلب من قبل المتطوع بعد تقديم الخدمة
	/// </summary>
	Task<VolunteerRequest?> CompleteRequestAsync(int requestId, string volunteerUserId);

	/// <summary>
	/// جلب الطلبات التي تعامل معها المتطوع
	/// </summary>
	Task<IEnumerable<VolunteerRequest>> GetRequestsHandledByVolunteerAsync(string volunteerUserId);
}

public class VolunteerRepository : IVolunteerRepository
{
	private readonly AppDbContext _context;

	public VolunteerRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<VolunteerRequest> CreateRequestAsync(int patientId, VolunteerRequestType type, string description)
	{
		var request = new VolunteerRequest
		{
			PatientId = patientId,
			Type = type,
			Description = description,
			Status = RequestStatus.Pending,
			CreatedAt = DateTime.Now
		};

		_context.VolunteerRequests.Add(request);
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<IEnumerable<VolunteerRequest>> GetAvailableRequestsForVolunteerAsync(string volunteerUserId)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);

		if (volunteer is null) return Enumerable.Empty<VolunteerRequest>();

		return await _context.VolunteerRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Where(r => 
				r.Status == RequestStatus.Pending && 
				r.Patient.GovernorateId == volunteer.GovernorateId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();
	}

	public async Task<VolunteerRequest?> AcceptRequestAsync(int requestId, string volunteerUserId)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);

		if (volunteer is null) return null;

		var request = await _context.VolunteerRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.User)
			.Where(r => 
				r.Id == requestId && 
				r.Patient.GovernorateId == volunteer.GovernorateId &&
				r.Status == RequestStatus.Pending)
			.FirstOrDefaultAsync();

		if (request is null) return null;

		request.VolunteerId = volunteer.Id;
		request.Status = RequestStatus.Accepted;

		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<VolunteerRequest?> FulfillFinancialDonationAsync(int requestId, string volunteerUserId, decimal amount, PaymentMethod paymentMethod, string? receiptUrl)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);

		if (volunteer is null) return null;

		var request = await _context.VolunteerRequests
			.FirstOrDefaultAsync(r => r.Id == requestId && r.Status == RequestStatus.Pending && r.Type == VolunteerRequestType.FinancialDonation);

		if (request is null) return null;

		request.VolunteerId = volunteer.Id;
		request.Amount = amount;
		request.PaymentMethod = paymentMethod;
		request.ReceiptUrl = receiptUrl;
		request.Status = RequestStatus.Pending; // Keeping it pending until admin/system approval

		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<IEnumerable<VolunteerRequest>> GetMyRequestsAsync(string patientUserId)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(p => p.IdentityUserId == patientUserId);

		if (patient is null) return Enumerable.Empty<VolunteerRequest>();

		return await _context.VolunteerRequests
			.Include(r => r.Volunteer)
			.Where(r => r.PatientId == patient.Id)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();
	}

	public async Task<VolunteerRequest?> CompleteRequestAsync(int requestId, string volunteerUserId)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);

		if (volunteer is null) return null;

		// التحقق أن المتطوع هو من قبل الطلب وأنه مقبول
		var request = await _context.VolunteerRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.User)
			.Where(r =>
				r.Id == requestId &&
				r.VolunteerId == volunteer.Id &&
				r.Status == RequestStatus.Accepted)
			.FirstOrDefaultAsync();

		if (request is null) return null;

		request.Status = RequestStatus.Completed;
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<IEnumerable<VolunteerRequest>> GetRequestsHandledByVolunteerAsync(string volunteerUserId)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);

		if (volunteer is null) return Enumerable.Empty<VolunteerRequest>();

		return await _context.VolunteerRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Where(r => r.VolunteerId == volunteer.Id)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();
	}
}
