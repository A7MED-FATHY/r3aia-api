using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Repositories;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
	private readonly IDeliveryRepository _deliveryRepository;

	public DeliveryController(IDeliveryRepository deliveryRepository)
	{
		_deliveryRepository = deliveryRepository;
	}

	[HttpGet("available")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetAvailable()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var tasks = await _deliveryRepository.GetAvailableTasksForVolunteerAsync(userId);
		return Ok(tasks);
	}

	[HttpPost("accept")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> AcceptTask([FromBody] AcceptTaskDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var task = await _deliveryRepository.AcceptTaskAsync(dto, userId);
		if (task is null) return BadRequest("Cannot accept this task.");

		return Ok(task);
	}

	[HttpPut("status")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> UpdateStatus([FromBody] UpdateTaskStatusDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var task = await _deliveryRepository.UpdateTaskStatusAsync(dto, userId);
		if (task is null) return BadRequest("Task not found or not assigned to you.");

		return Ok(task);
	}
}


