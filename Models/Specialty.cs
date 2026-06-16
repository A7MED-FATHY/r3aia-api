using System.ComponentModel.DataAnnotations;

namespace R3AIA.Models
{
	public class Specialty
	{
		public int Id { get; set; }
		[Required]
		public string Name { get; set; } = null!;
	}
}