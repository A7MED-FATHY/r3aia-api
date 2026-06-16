using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Repositories;

public interface IDeliveryRepository
{
	Task<DeliveryTask?> AcceptTaskAsync(AcceptTaskDto dto, string volunteerUserId);
	Task<DeliveryTask?> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, string volunteerUserId);
	Task<IEnumerable<DeliveryTask>> GetAvailableTasksForVolunteerAsync(string volunteerUserId);
}

public class DeliveryRepository : IDeliveryRepository
{
	private readonly AppDbContext _context;

	public DeliveryRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<DeliveryTask?> AcceptTaskAsync(AcceptTaskDto dto, string volunteerUserId)
	{
		// نجيب المتطوع المرتبط بالـ ApplicationUser
		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);
		if (volunteer is null) return null;

		// نتاكد من الطلب
		var request = await _context.MedicineRequests.FindAsync(dto.RequestId);
		if (request is null) return null;

		// إنشاء أو تحديث مهمة التوصيل
		var existingTask = await _context.DeliveryTasks
			.FirstOrDefaultAsync(t => t.MedicineRequestId == dto.RequestId);

		if (existingTask is null)
		{
			existingTask = new DeliveryTask
			{
				MedicineRequestId = dto.RequestId,
				VolunteerId = volunteer.Id,
				TaskStatus = DeliveryStatus.Taken
			};
			_context.DeliveryTasks.Add(existingTask);
		}
		else
		{
			if (existingTask.TaskStatus != DeliveryStatus.Available)
				return null;

			existingTask.VolunteerId = volunteer.Id;
			existingTask.TaskStatus = DeliveryStatus.Taken;
		}

		// تحديث حالة الطلب الأصلي (اعتباره قيد التنفيذ من المتطوع)
		request.RequestStatus = RequestStatus.Accepted;

		await _context.SaveChangesAsync();
		return existingTask;
	}

	public async Task<DeliveryTask?> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, string volunteerUserId)
	{
		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);
		if (volunteer is null) return null;

		var task = await _context.DeliveryTasks
			.Include(t => t.MedicineRequest)
			.FirstOrDefaultAsync(t => t.Id == dto.TaskId && t.VolunteerId == volunteer.Id);

		if (task is null) return null;

		task.TaskStatus = dto.Status;

		// لو تم التوصيل، نغلق الطلب
		if (dto.Status == DeliveryStatus.Delivered)
		{
			task.MedicineRequest.RequestStatus = RequestStatus.Completed;
		}

		await _context.SaveChangesAsync();
		return task;
	}

	public async Task<IEnumerable<DeliveryTask>> GetAvailableTasksForVolunteerAsync(string volunteerUserId)
	{
		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);
		if (volunteer is null)
		{
			return Enumerable.Empty<DeliveryTask>();
		}

		return await _context.DeliveryTasks
			.Include(t => t.MedicineRequest)
				.ThenInclude(r => r.Patient)
					.ThenInclude(p => p.City)
			.Where(t =>
				t.TaskStatus == DeliveryStatus.Available &&
				t.MedicineRequest.Patient.GovernorateId == volunteer.GovernorateId)
			.ToListAsync();
	}
}


