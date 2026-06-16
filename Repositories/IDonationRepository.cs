using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;

namespace R3AIA.Repositories;

public interface IDonationRepository
{
	Task<IEnumerable<DonationCase>> GetOpenCasesAsync();
	Task<Donation?> AddDonationAsync(CreateDonationDto dto, string userId, string? receiptUrl);
}

public class DonationRepository : IDonationRepository
{
	private readonly AppDbContext _context;

	public DonationRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<DonationCase>> GetOpenCasesAsync()
	{
		return await _context.DonationCases
			.Where(c => !c.IsCompleted)
			.ToListAsync();
	}

	public async Task<Donation?> AddDonationAsync(CreateDonationDto dto, string userId, string? receiptUrl)
	{
		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
		
		var donationCase = await _context.DonationCases.FindAsync(dto.CaseId);
		if (donationCase is null || donationCase.IsCompleted) return null;

		var donation = new Donation
		{
			VolunteerId = volunteer?.Id,
			DonorUserId = userId,
			CaseId = dto.CaseId,
			Amount = dto.Amount,
			ReceiptImage = receiptUrl ?? "",
			CreatedAt = DateTime.Now
		};

		_context.Donations.Add(donation);

		// Auto-update CollectedAmount
		donationCase.CollectedAmount += dto.Amount;
		if (donationCase.CollectedAmount >= donationCase.GoalAmount)
		{
			donationCase.IsCompleted = true;
		}

		await _context.SaveChangesAsync();
		return donation;
	}
}


