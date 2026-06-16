using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using R3AIA.DTOs;
using System.Security.Claims;

namespace R3AIA.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class NotificationsController : ControllerBase
	{
		private readonly AppDbContext _context;

		public NotificationsController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> GetNotifications()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var notifications = await _context.Notifications
				.Where(n => n.UserId == userId)
				.OrderByDescending(n => n.CreatedAt)
				.Select(n => new NotificationDisplayDto
				{
					Id = n.Id,
					Title = n.Title,
					Message = n.Message,
					IsRead = n.IsRead,
					CreatedAt = n.CreatedAt
				})
				.ToListAsync();

			return Ok(notifications);
		}

		[HttpPut("{id}/read")]
		public async Task<IActionResult> MarkAsRead(int id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var notification = await _context.Notifications
				.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

			if (notification == null) return NotFound();

			notification.IsRead = true;
			await _context.SaveChangesAsync();

			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteNotification(int id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var notification = await _context.Notifications
				.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

			if (notification == null) return NotFound();

			_context.Notifications.Remove(notification);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Notification deleted successfully." });
		}

		[HttpPut("read-all")]
		public async Task<IActionResult> MarkAllAsRead()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var unreadNotifications = await _context.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.ToListAsync();

			foreach (var notification in unreadNotifications)
			{
				notification.IsRead = true;
			}

			await _context.SaveChangesAsync();

			return Ok(new { message = "All notifications marked as read." });
		}
	}
}
