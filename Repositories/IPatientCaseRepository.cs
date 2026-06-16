using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;
using static R3AIA.Models.Enums;

namespace R3AIA.Repositories;

// ── Interface ────────────────────────────────────────────────────────────────

public interface IPatientCaseRepository
{
    // Public
    Task<IEnumerable<PatientCase>> GetApprovedCasesAsync();
    Task<PatientCase?> GetCaseByIdAsync(int id);

    // Admin - Cases
    Task<IEnumerable<PatientCase>> GetAllCasesAsync();
    Task<PatientCase> CreateCaseAsync(CreatePatientCaseDto dto, List<string> imageUrls);
    Task<PatientCase?> UpdateCaseAsync(int id, UpdatePatientCaseDto dto, List<string>? newImageUrls);
    Task<bool> DeleteCaseAsync(int id);
    Task<bool> ApproveCaseAsync(int id);

    // Donations
    Task<PatientDonation?> CreateDonationAsync(CreatePatientDonationDto dto, string? userId, string? proofImageUrl = null);
    Task<IEnumerable<PatientDonation>> GetAllDonationsAsync();
    Task<bool> ApproveDonationAsync(int id, INotificationService notificationService);
    Task<bool> RejectDonationAsync(int id);
}

// ── Implementation ───────────────────────────────────────────────────────────

public class PatientCaseRepository : IPatientCaseRepository
{
    private readonly AppDbContext _context;

    public PatientCaseRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Public ───────────────────────────────────────────────────────────────

    public async Task<IEnumerable<PatientCase>> GetApprovedCasesAsync()
    {
        return await _context.PatientCases
            .Include(c => c.Governorate)
            .Where(c => c.Status == CaseStatus.Approved)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<PatientCase?> GetCaseByIdAsync(int id)
    {
        return await _context.PatientCases
            .Include(c => c.Governorate)
            .Include(c => c.Donations.OrderByDescending(d => d.CreatedAt).Take(10))
            .FirstOrDefaultAsync(c => c.Id == id && c.Status == CaseStatus.Approved);
    }

    // ── Admin – Cases ────────────────────────────────────────────────────────

    public async Task<IEnumerable<PatientCase>> GetAllCasesAsync()
    {
        return await _context.PatientCases
            .Include(c => c.Governorate)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<PatientCase> CreateCaseAsync(CreatePatientCaseDto dto, List<string> imageUrls)
    {
        var patientCase = new PatientCase
        {
            Title = dto.Title,
            Description = dto.Description,
            GovernorateId = dto.GovernorateId,
            CaseType = dto.CaseType,
            RequiredAmount = dto.RequiredAmount,
            Status = CaseStatus.Pending,
            ExpiryDate = dto.ExpiryDate,
            Images = imageUrls,
            CreatedAt = DateTime.Now
        };

        _context.PatientCases.Add(patientCase);
        await _context.SaveChangesAsync();
        return patientCase;
    }

    public async Task<PatientCase?> UpdateCaseAsync(int id, UpdatePatientCaseDto dto, List<string>? newImageUrls)
    {
        var patientCase = await _context.PatientCases.FindAsync(id);
        if (patientCase is null) return null;

        if (dto.Title is not null) patientCase.Title = dto.Title;
        if (dto.Description is not null) patientCase.Description = dto.Description;
        if (dto.GovernorateId.HasValue) patientCase.GovernorateId = dto.GovernorateId;
        if (dto.CaseType.HasValue) patientCase.CaseType = dto.CaseType.Value;
        if (dto.RequiredAmount.HasValue) patientCase.RequiredAmount = dto.RequiredAmount.Value;
        if (dto.ExpiryDate.HasValue) patientCase.ExpiryDate = dto.ExpiryDate;
        if (newImageUrls is not null && newImageUrls.Count > 0)
            patientCase.Images = newImageUrls;

        await _context.SaveChangesAsync();
        return patientCase;
    }

    public async Task<bool> DeleteCaseAsync(int id)
    {
        var patientCase = await _context.PatientCases.FindAsync(id);
        if (patientCase is null) return false;

        _context.PatientCases.Remove(patientCase);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveCaseAsync(int id)
    {
        var patientCase = await _context.PatientCases.FindAsync(id);
        if (patientCase is null) return false;
        if (patientCase.Status != CaseStatus.Pending) return false;

        patientCase.Status = CaseStatus.Approved;
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Donations ────────────────────────────────────────────────────────────

    public async Task<PatientDonation?> CreateDonationAsync(CreatePatientDonationDto dto, string? userId, string? proofImageUrl = null)
    {
        var patientCase = await _context.PatientCases.FindAsync(dto.PatientCaseId);
        if (patientCase is null || patientCase.Status != CaseStatus.Approved)
            return null;

        if (patientCase.CollectedAmount >= patientCase.RequiredAmount)
            return null; // Case already completed

        if (dto.Amount <= 0) return null;

        string? donorName = dto.DonorName;

        // If user is authenticated, use their full name from DB
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _context.Users.FindAsync(userId);
            donorName ??= user?.FullName;
        }

        var donation = new PatientDonation
        {
            PatientCaseId = dto.PatientCaseId,
            DonorId = string.IsNullOrEmpty(userId) ? null : userId,
            DonorName = string.IsNullOrEmpty(donorName) ? "متبرع مجهول" : donorName,
            DonorPhone = dto.DonorPhone,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ProofImage = proofImageUrl,
            Status = DonationStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _context.PatientDonations.Add(donation);
        await _context.SaveChangesAsync();
        return donation;
    }

    public async Task<IEnumerable<PatientDonation>> GetAllDonationsAsync()
    {
        return await _context.PatientDonations
            .Include(d => d.Donor)
            .Include(d => d.PatientCase)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveDonationAsync(int id, INotificationService notificationService)
    {
        var donation = await _context.PatientDonations
            .Include(d => d.PatientCase)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (donation is null) return false;
        if (donation.Status == DonationStatus.Approved) return false; // prevent double approval

        donation.Status = DonationStatus.Approved;

        var patientCase = donation.PatientCase;
        patientCase.CollectedAmount += donation.Amount;

        if (patientCase.CollectedAmount >= patientCase.RequiredAmount)
            patientCase.Status = CaseStatus.Completed;

        await _context.SaveChangesAsync();

        // Notify donor (if registered user)
        if (!string.IsNullOrEmpty(donation.DonorId))
        {
            await notificationService.SendNotificationAsync(
                donation.DonorId,
                "تم قبول تبرعك ✅",
                $"تم قبول تبرعك بمبلغ {donation.Amount} جنيه لصالح حالة \"{patientCase.Title}\". جزاك الله خيراً!");
        }

        return true;
    }

    public async Task<bool> RejectDonationAsync(int id)
    {
        var donation = await _context.PatientDonations.FindAsync(id);
        if (donation is null) return false;
        if (donation.Status != DonationStatus.Pending) return false;

        donation.Status = DonationStatus.Rejected;
        await _context.SaveChangesAsync();
        return true;
    }
}
