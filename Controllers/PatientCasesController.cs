using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Repositories;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/cases")]
public class PatientCasesController : ControllerBase
{
    private readonly IPatientCaseRepository _repo;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public PatientCasesController(IPatientCaseRepository repo, IFileService fileService, IMapper mapper)
    {
        _repo = repo;
        _fileService = fileService;
        _mapper = mapper;
    }

    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>GET /api/cases — Approved cases only (public)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetApprovedCases()
    {
        var cases = await _repo.GetApprovedCasesAsync();
        var result = _mapper.Map<IEnumerable<PatientCaseSummaryDto>>(cases);
        return Ok(result);
    }

    /// <summary>GET /api/cases/{id} — Single approved case with recent donations</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCaseById(int id)
    {
        var patientCase = await _repo.GetCaseByIdAsync(id);
        if (patientCase is null) return NotFound("الحالة غير موجودة أو لم تتم الموافقة عليها بعد.");
        var result = _mapper.Map<PatientCaseDetailDto>(patientCase);
        return Ok(result);
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/cases/admin/all — All cases (Admin)</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllCases()
    {
        var cases = await _repo.GetAllCasesAsync();
        var result = _mapper.Map<IEnumerable<PatientCaseSummaryDto>>(cases);
        return Ok(result);
    }

    /// <summary>POST /api/cases — Create case (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCase([FromForm] CreatePatientCaseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var imageUrls = new List<string>();
        if (dto.Images != null)
        {
            foreach (var image in dto.Images.Take(5))
            {
                var url = await _fileService.SaveImageAsync(image, "Uploads/Cases");
                imageUrls.Add(url);
            }
        }

        var createdCase = await _repo.CreateCaseAsync(dto, imageUrls);
        var result = _mapper.Map<PatientCaseSummaryDto>(createdCase);
        return CreatedAtAction(nameof(GetCaseById), new { id = createdCase.Id }, result);
    }

    /// <summary>PUT /api/cases/{id} — Update case (Admin)</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCase(int id, [FromForm] UpdatePatientCaseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        List<string>? imageUrls = null;
        if (dto.Images != null && dto.Images.Any())
        {
            imageUrls = new List<string>();
            foreach (var image in dto.Images.Take(5))
            {
                var url = await _fileService.SaveImageAsync(image, "Uploads/Cases");
                imageUrls.Add(url);
            }
        }

        var updated = await _repo.UpdateCaseAsync(id, dto, imageUrls);
        if (updated is null) return NotFound("الحالة غير موجودة.");
        var result = _mapper.Map<PatientCaseSummaryDto>(updated);
        return Ok(result);
    }

    /// <summary>DELETE /api/cases/{id} — Delete case (Admin)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCase(int id)
    {
        var deleted = await _repo.DeleteCaseAsync(id);
        if (!deleted) return NotFound("الحالة غير موجودة.");
        return Ok(new { message = "تم حذف الحالة بنجاح." });
    }

    /// <summary>POST /api/cases/{id}/approve — Approve case (Admin)</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveCase(int id)
    {
        var approved = await _repo.ApproveCaseAsync(id);
        if (!approved) return BadRequest("تعذّر الموافقة على الحالة. تأكد أنها موجودة وفي حالة Pending.");
        return Ok(new { message = "تمت الموافقة على الحالة وأصبحت ظاهرة للمستخدمين." });
    }
}
