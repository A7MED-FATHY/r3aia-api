using R3AIA.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class Doctor
	{
		public int Id { get; set; }

		[Required]
		public string IdentityUserId { get; set; } = null!;
		[ForeignKey("IdentityUserId")]
		public virtual ApplicationUser User { get; set; } = null!;

		[Required]
		public string FullName { get; set; } = null!;
		public string PhoneNumber { get; set; } = null!;

		public int SpecialtyId { get; set; }
		[ForeignKey("SpecialtyId")]
		public virtual Specialty Specialty { get; set; } = null!;

		public int GovernorateId { get; set; }
		[ForeignKey("GovernorateId")]
		public virtual Governorate Governorate { get; set; } = null!;

		public int? CityId { get; set; }
		[ForeignKey("CityId")]
		public virtual City? City { get; set; }

		public string? Address { get; set; }
		public string? NIDImage { get; set; }
		public string ClinicAddress { get; set; } = null!;
		public string? ProfilePictureUrl { get; set; }

		public bool IsVerified { get; set; } = false;
	}
}