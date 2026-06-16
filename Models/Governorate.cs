using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R3AIA.Models
{
	public class Governorate
	{
		public int Id { get; set; }
		[Required]
		public string Name { get; set; } = null!;

		// Navigation
		public virtual ICollection<City> Cities { get; set; } = new List<City>();
	}
}