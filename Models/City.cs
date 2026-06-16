using R3AIA.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class City
	{
		public int Id { get; set; }
		[Required]
		public string Name { get; set; } = null!;

		public int GovernorateId { get; set; }
		[ForeignKey("GovernorateId")]
		public virtual Governorate Governorate { get; set; } = null!;
	}
}