using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Repositories;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/donations")]
public class PatientDonationsController : ControllerBase
{
    private readonly IPatientCaseRepository _repo;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public PatientDonationsController(
        IPatientCaseRepository repo,
        IFileService fileService,
        INotificationService notificationService,
        IMapper mapper)
    {
        _repo = repo;
        _fileService = fileService;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    /// <summary>
    /// POST /api/donations — Create a new donation (User or Guest)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateDonation([FromForm] CreatePatientDonationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Extract userId from JWT (null for guests)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub")?.Value;

        // Save proof image first
        string? proofUrl = null;
        if (dto.ProofImage != null)
            proofUrl = await _fileService.SaveImageAsync(dto.ProofImage, "Uploads/Proofs");

        var donation = await _repo.CreateDonationAsync(dto, userId, proofUrl);
        if (donation is null)
            return BadRequest("تعذّر إنشاء التبرع. تأكد أن الحالة موافق عليها ولم تكتمل بعد.");

        var result = _mapper.Map<PatientDonationSummaryDto>(donation);
        return Ok(new
        {
            message = "تم استلام تبرعك بنجاح وهو قيد المراجعة. جزاك الله خيراً! 🌟",
            data = result
        });
    }

    /// <summary>
    /// GET /api/donations/admin — All donations for admin review
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDonations()
    {
        var donations = await _repo.GetAllDonationsAsync();
        var result = _mapper.Map<IEnumerable<AdminPatientDonationDto>>(donations);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/donations/{id}/approve — Admin approves donation
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveDonation(int id)
    {
        var approved = await _repo.ApproveDonationAsync(id, _notificationService);
        if (!approved)
            return BadRequest("تعذّر قبول التبرع. ربما تم قبوله مسبقاً أو غير موجود.");
        return Ok(new { message = "تم قبول التبرع وإضافة المبلغ للحالة." });
    }

    /// <summary>
    /// POST /api/donations/{id}/reject — Admin rejects donation
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectDonation(int id)
    {
        var rejected = await _repo.RejectDonationAsync(id);
        if (!rejected)
            return BadRequest("تعذّر رفض التبرع. ربما لم يعد في حالة معلقة.");
        return Ok(new { message = "تم رفض التبرع." });
    }
}
