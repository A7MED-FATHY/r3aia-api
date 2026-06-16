using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class DonationCase
	{
		public int Id { get; set; }
		public string Title { get; set; } = null!;
		public string Description { get; set; } = null!;
		public string CaseImage { get; set; } = null!;
		public string PatientName { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; } = DateTime.Now;

		[Column(TypeName = "decimal(18,2)")]
		public decimal GoalAmount { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal CollectedAmount { get; set; } = 0;

		public bool IsCompleted { get; set; } = false;
	}
}