using Microsoft.EntityFrameworkCore;
using R3AIA.Models;

namespace R3AIA.Services;

public interface INotificationService
{
	/// <summary>
	/// إرسال إشعار لمستخدم محدد
	/// </summary>
	Task SendNotificationAsync(string userId, string title, string message);
	Task SendToUserAsync(string userId, string title, string message); // alias
	Task SendToRoleAsync(Enums.UserType role, string title, string message);
	Task BroadcastAsync(string title, string message);
}

public class NotificationService : INotificationService
{
	private readonly AppDbContext _context;

	public NotificationService(AppDbContext context)
	{
		_context = context;
	}

	public async Task SendNotificationAsync(string userId, string title, string message)
	{
		var notification = new Notification
		{
			UserId = userId,
			Title = title,
			Message = message,
			IsRead = false,
			CreatedAt = DateTime.Now
		};

		_context.Notifications.Add(notification);
		await _context.SaveChangesAsync();
	}

	public Task SendToUserAsync(string userId, string title, string message)
		=> SendNotificationAsync(userId, title, message);

	public async Task SendToRoleAsync(Enums.UserType role, string title, string message)
	{
		var userIds = await _context.Users
			.Where(u => u.UserType == role)
			.Select(u => u.Id)
			.ToListAsync();

		var notifications = userIds.Select(userId => new Notification
		{
			UserId = userId,
			Title = title,
			Message = message,
			CreatedAt = DateTime.Now
		});

		_context.Notifications.AddRange(notifications);
		await _context.SaveChangesAsync();
	}

	public async Task BroadcastAsync(string title, string message)
	{
		var userIds = await _context.Users
			.Select(u => u.Id)
			.ToListAsync();

		var notifications = userIds.Select(userId => new Notification
		{
			UserId = userId,
			Title = title,
			Message = message,
			CreatedAt = DateTime.Now
		});

		_context.Notifications.AddRange(notifications);
		await _context.SaveChangesAsync();
	}
}
