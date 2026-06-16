using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Repositories;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/Donations")]
public class DonationsController : ControllerBase
{
	private readonly IDonationRepository _donationRepository;
	private readonly IFileService _fileService;
	private readonly IMapper _mapper;

	public DonationsController(
		IDonationRepository donationRepository,
		IFileService fileService,
		IMapper mapper)
	{
		_donationRepository = donationRepository;
		_fileService = fileService;
		_mapper = mapper;
	}

	[HttpGet("cases")]
	[AllowAnonymous]
	public async Task<IActionResult> GetCases()
	{
		var cases = await _donationRepository.GetOpenCasesAsync();
		var result = _mapper.Map<IEnumerable<DonationCaseSummaryDto>>(cases);
		return Ok(result);
	}

	[HttpPost("pay")]
	[Authorize]
	public async Task<IActionResult> Pay([FromForm] CreateDonationDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
			if (userId is null) return Unauthorized();

			string? receiptUrl = null;
			if (dto.ReceiptImage != null)
			{
				receiptUrl = await _fileService.SaveImageAsync(dto.ReceiptImage, "Uploads/Donations");
			}

			var donation = await _donationRepository.AddDonationAsync(dto, userId, receiptUrl);
			if (donation is null) return BadRequest("Cannot add donation.");

			var result = _mapper.Map<DonationResultDto>(donation);
			return Ok(result);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
		}
	}
}


