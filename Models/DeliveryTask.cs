using R3AIA.Models;
using System.ComponentModel.DataAnnotations.Schema;
using static R3AIA.Models.Enums;

namespace R3AIA.Models
{
	public class DeliveryTask
	{
		public int Id { get; set; }

		public int MedicineRequestId { get; set; }
		[ForeignKey("MedicineRequestId")]
		public virtual MedicineRequest MedicineRequest { get; set; } = null!;

		public int? VolunteerId { get; set; }
		[ForeignKey("VolunteerId")]
		public Volunteer? Volunteer { get; set; }

		
		public DeliveryStatus TaskStatus { get; set; } = DeliveryStatus.Available;

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}