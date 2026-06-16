using System.ComponentModel.DataAnnotations;
using static R3AIA.Models.Enums;

namespace R3AIA.DTOs;

public class AcceptTaskDto
{
	[Required]
	public int RequestId { get; set; }
}

public class UpdateTaskStatusDto
{
	[Required]
	public int TaskId { get; set; }

	[Required]
	public DeliveryStatus Status { get; set; }
}


