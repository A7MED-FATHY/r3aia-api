using R3AIA.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class Notification
	{
		public int Id { get; set; }

		public string UserId { get; set; } = null!;
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; } = null!;

		public string Title { get; set; } = null!;
		public string Message { get; set; } = null!;
		public bool IsRead { get; set; } = false;
		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}