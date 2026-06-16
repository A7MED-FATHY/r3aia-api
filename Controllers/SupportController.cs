using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Repositories;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportController : ControllerBase
{
	private readonly ISupportRepository _supportRepository;
	private readonly IMapper _mapper;
	private readonly UserManager<ApplicationUser> _userManager;

	public SupportController(ISupportRepository supportRepository, IMapper mapper, UserManager<ApplicationUser> userManager)
	{
		_supportRepository = supportRepository;
		_mapper = mapper;
		_userManager = userManager;
	}

	[HttpPost("report")]
	[Authorize]
	public async Task<IActionResult> Report([FromBody] CreateReportDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var report = await _supportRepository.AddReportAsync(dto, userId);
		return Ok(report);
	}

	[HttpGet("notifs")]
	[Authorize]
	public async Task<IActionResult> GetNotifications()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var notifications = await _supportRepository.GetUserNotificationsAsync(userId);
		var result = _mapper.Map<IEnumerable<NotificationDto>>(notifications);
		return Ok(result);
	}

	[HttpPatch("mark-read/{id}")]
	[Authorize]
	public async Task<IActionResult> MarkRead(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var ok = await _supportRepository.MarkAsReadAsync(id, userId);
		if (!ok) return NotFound();

		return Ok();
	}

	[HttpPatch("mark-all-read")]
	[Authorize]
	public async Task<IActionResult> MarkAllRead()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		await _supportRepository.MarkAllAsReadAsync(userId);
		return Ok();
	}

	[HttpDelete("delete-notif/{id}")]
	[Authorize]
	public async Task<IActionResult> DeleteNotification(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var ok = await _supportRepository.DeleteNotificationAsync(id, userId);
		if (!ok) return NotFound();

		return Ok();
	}

	[HttpPost("contact")]
	[AllowAnonymous]
	public async Task<IActionResult> Contact([FromBody] ContactMessageDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var message = await _supportRepository.AddSupportMessageAsync(dto);
		return Ok(new { message = "Message sent successfully", id = message.Id });
	}

	[HttpGet("my-messages")]
	[AllowAnonymous]
	public async Task<IActionResult> GetMyMessages([FromQuery] string? email)
	{
		var userEmail = email;
		if (string.IsNullOrEmpty(userEmail))
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
			if (userId != null)
			{
				var user = await _userManager.FindByIdAsync(userId);
				userEmail = user?.Email;
			}
		}

		if (string.IsNullOrEmpty(userEmail)) return BadRequest("Email is required for guest users.");

		var messages = await _supportRepository.GetSupportMessagesByEmailAsync(userEmail);
		return Ok(messages);
	}

	[HttpPost("create")]
	[Authorize]
	public async Task<IActionResult> CreateTicket([FromBody] CreateSupportTicketDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId == null) return Unauthorized();

		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return Unauthorized();

		var ticket = await _supportRepository.CreateTicketAsync(userId, user.UserType, dto);
		return Ok(new { message = "Ticket created successfully", id = ticket.Id });
	}

	[HttpGet("my-tickets")]
	[Authorize]
	public async Task<IActionResult> GetMyTickets()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId == null) return Unauthorized();

		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return Unauthorized();

		var tickets = await _supportRepository.GetUserTicketsAsync(userId);
		var result = tickets.Select(t => new SupportTicketReturnDto
		{
			Id = t.Id,
			UserId = t.UserId,
			TargetUserFullName = user.FullName,
			UserType = t.UserType.ToString(),
			Subject = t.Subject,
			Message = t.Message,
			Status = t.Status.ToString(),
			CreatedAt = t.CreatedAt
		}).ToList();

		return Ok(result);
	}

	[HttpGet("{ticketId}")]
	[Authorize]
	public async Task<IActionResult> GetTicket(int ticketId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId == null) return Unauthorized();

		var ticket = await _supportRepository.GetTicketByIdAsync(ticketId);
		if (ticket == null || ticket.UserId != userId) return NotFound();

		var ticketUser = await _userManager.FindByIdAsync(ticket.UserId);

		var result = new SupportTicketReturnDto
		{
			Id = ticket.Id,
			UserId = ticket.UserId,
			TargetUserFullName = ticketUser?.FullName ?? "Unknown",
			UserType = ticket.UserType.ToString(),
			Subject = ticket.Subject,
			Message = ticket.Message,
			Status = ticket.Status.ToString(),
			CreatedAt = ticket.CreatedAt,
			Replies = ticket.Replies.Select(r => new SupportReplyReturnDto
			{
				Id = r.Id,
				TicketId = r.TicketId,
				SenderId = r.SenderId,
				SenderType = r.SenderType,
				Message = r.Message,
				CreatedAt = r.CreatedAt
			}).OrderBy(r => r.CreatedAt).ToList()
		};

		return Ok(result);
	}

	[HttpPost("reply")]
	[Authorize]
	public async Task<IActionResult> ReplyTicket([FromQuery] int ticketId, [FromBody] ReplySupportTicketDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId == null) return Unauthorized();

		var ticket = await _supportRepository.GetTicketByIdAsync(ticketId);
		if (ticket == null || ticket.UserId != userId) return NotFound();

		var reply = await _supportRepository.ReplyToTicketAsync(ticketId, userId, "User", dto.Message);
		if (reply == null) return NotFound();
		
		return Ok(new { message = "Reply sent successfully", id = reply.Id });
	}
}


