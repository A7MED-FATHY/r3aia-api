using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;

namespace R3AIA.Repositories;

public interface ISupportRepository
{
	Task<UserReport> AddReportAsync(CreateReportDto dto, string reporterUserId);
	Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
	Task PushNotificationAsync(string userId, string message);
	Task<bool> MarkAsReadAsync(int notificationId, string userId);
	Task MarkAllAsReadAsync(string userId);
	Task<bool> DeleteNotificationAsync(int notificationId, string userId);
	Task<SupportMessage> AddSupportMessageAsync(ContactMessageDto dto);
	Task<IEnumerable<SupportMessage>> GetAllSupportMessagesAsync();
	Task<bool> ReplyToSupportMessageAsync(int messageId, string reply);
	
	Task<SupportTicket> CreateTicketAsync(string userId, Enums.UserType userType, CreateSupportTicketDto dto);
	Task<IEnumerable<SupportTicket>> GetUserTicketsAsync(string userId);
	Task<SupportTicket?> GetTicketByIdAsync(int ticketId);
	Task<SupportReply?> ReplyToTicketAsync(int ticketId, string senderId, string senderType, string message);
	Task<IEnumerable<SupportTicket>> GetAllTicketsAsync();
	Task<bool> CloseTicketAsync(int ticketId);
	Task<IEnumerable<SupportMessage>> GetSupportMessagesByEmailAsync(string email);
	Task NotifyAllAdminsAsync(string title, string message);
}

public class SupportRepository : ISupportRepository
{
	private readonly AppDbContext _context;

	public SupportRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<UserReport> AddReportAsync(CreateReportDto dto, string reporterUserId)
	{
		var report = new UserReport
		{
			ReporterId = reporterUserId,
			ReportedUserId = dto.ReportedUserId,
			Reason = dto.Reason
		};

		_context.UserReports.Add(report);

		// إشعار للمستخدم المُبلغ عنه
		await PushNotificationAsync(dto.ReportedUserId, "لديك بلاغ جديد.");

		// إشعار للمسؤولين
		await NotifyAllAdminsAsync("بلاغ جديد", "تم تقديم بلاغ جديد يحتاج للمراجعة.");

		await _context.SaveChangesAsync();
		return report;
	}

	public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
	{
		return await _context.Notifications
			.Where(n => n.UserId == userId)
			.OrderByDescending(n => n.CreatedAt)
			.ToListAsync();
	}

	public async Task PushNotificationAsync(string userId, string message)
	{
		var notification = new Notification
		{
			UserId = userId,
			Title = "R3AIA",
			Message = message
		};

		_context.Notifications.Add(notification);
		await _context.SaveChangesAsync();
	}

	public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
	{
		var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
		if (notif == null) return false;

		notif.IsRead = true;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task MarkAllAsReadAsync(string userId)
	{
		var unreadNotifs = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
		foreach (var n in unreadNotifs)
		{
			n.IsRead = true;
		}
		await _context.SaveChangesAsync();
	}

	public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
	{
		var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
		if (notif == null) return false;

		_context.Notifications.Remove(notif);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<SupportMessage> AddSupportMessageAsync(ContactMessageDto dto)
	{
		var message = new SupportMessage
		{
			FullName = dto.FullName,
			Email = dto.Email,
			Category = dto.Category,
			Message = dto.Message
		};

		_context.SupportMessages.Add(message);
		
		// إشعار للمسؤولين
		await NotifyAllAdminsAsync("رسالة تواصل جديدة", $"رسالة من {dto.FullName}: {dto.Category}");

		await _context.SaveChangesAsync();
		return message;
	}

	public async Task<IEnumerable<SupportMessage>> GetAllSupportMessagesAsync()
	{
		return await _context.SupportMessages
			.OrderByDescending(m => m.CreatedAt)
			.ToListAsync();
	}

	public async Task<bool> ReplyToSupportMessageAsync(int messageId, string reply)
	{
		var msg = await _context.SupportMessages.FindAsync(messageId);
		if (msg == null) return false;

		msg.AdminReply = reply;
		msg.RepliedAt = DateTime.Now;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<SupportTicket> CreateTicketAsync(string userId, Enums.UserType userType, CreateSupportTicketDto dto)
	{
		var ticket = new SupportTicket
		{
			UserId = userId,
			UserType = userType,
			Subject = dto.Subject,
			Message = dto.Message,
			Status = Enums.SupportStatus.Open
		};
		_context.SupportTickets.Add(ticket);
		
		// إشعار للمسؤولين
		await NotifyAllAdminsAsync("تذكرة دعم جديدة", $"تذكرة من {userId}: {dto.Subject}");

		await _context.SaveChangesAsync();
		return ticket;
	}

	public async Task<IEnumerable<SupportTicket>> GetUserTicketsAsync(string userId)
	{
		return await _context.SupportTickets
			.Include(t => t.Replies)
			.Where(t => t.UserId == userId)
			.OrderByDescending(t => t.CreatedAt)
			.ToListAsync();
	}

	public async Task<SupportTicket?> GetTicketByIdAsync(int ticketId)
	{
		return await _context.SupportTickets
			.Include(t => t.Replies)
			.FirstOrDefaultAsync(t => t.Id == ticketId);
	}

	public async Task<SupportReply?> ReplyToTicketAsync(int ticketId, string senderId, string senderType, string message)
	{
		var ticket = await _context.SupportTickets.FindAsync(ticketId);
		if (ticket == null) return null;

		var reply = new SupportReply
		{
			TicketId = ticketId,
			SenderId = senderId,
			SenderType = senderType,
			Message = message
		};

		_context.SupportReplies.Add(reply);

		if (senderType == "Admin" && ticket.Status == Enums.SupportStatus.Open)
		{
			ticket.Status = Enums.SupportStatus.InProgress;
		}

		await _context.SaveChangesAsync();

		if (senderType == "Admin")
		{
			await PushNotificationAsync(ticket.UserId, "تم الرد على تذكرتك: " + ticket.Subject);
		}
		else
		{
			// إشعار للمسؤولين عند رد المستخدم
			await NotifyAllAdminsAsync("رد على تذكرة", $"رد جديد على تذكرة: {ticket.Subject}");
		}

		return reply;
	}

	public async Task<IEnumerable<SupportTicket>> GetAllTicketsAsync()
	{
		return await _context.SupportTickets
			.Include(t => t.Replies)
			.OrderByDescending(t => t.CreatedAt)
			.ToListAsync();
	}

	public async Task<bool> CloseTicketAsync(int ticketId)
	{
		var ticket = await _context.SupportTickets.FindAsync(ticketId);
		if (ticket == null) return false;

		ticket.Status = Enums.SupportStatus.Closed;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<IEnumerable<SupportMessage>> GetSupportMessagesByEmailAsync(string email)
	{
		return await _context.SupportMessages
			.Where(m => m.Email == email)
			.OrderByDescending(m => m.CreatedAt)
			.ToListAsync();
	}

	public async Task NotifyAllAdminsAsync(string title, string message)
	{
		var admins = await _context.Users
			.Where(u => u.UserType == Enums.UserType.Admin)
			.Select(u => u.Id)
			.ToListAsync();

		foreach (var adminId in admins)
		{
			var notification = new Notification
			{
				UserId = adminId,
				Title = title,
				Message = message,
				CreatedAt = DateTime.Now
			};
			_context.Notifications.Add(notification);
		}
		// SaveChanges will be called by the caller repository method
	}
}


