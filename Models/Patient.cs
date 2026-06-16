using R3AIA.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class Patient
	{
		public int Id { get; set; }

		[Required]
		public string IdentityUserId { get; set; } = null!;
		[ForeignKey("IdentityUserId")]
		public ApplicationUser User { get; set; } = null!;

		[Required]
		public string FullName { get; set; } = null!;

		[Required]
		public string NationalID { get; set; } = null!;
		public string PhoneNumber { get; set; } = null!;

		public int GovernorateId { get; set; }
		[ForeignKey("GovernorateId")] 
		public virtual Governorate Governorate { get; set; } = null!;
		public int CityId { get; set; }
		[ForeignKey("CityId")] 
		public virtual City City { get; set; } = null!; 
		public string Address { get; set; } = null!;

		public string NIDImage { get; set; } = null!;
		public string SocialProofImage { get; set; } = null!;

		public bool HasChronicDisease { get; set; }
		public bool IsVerified { get; set; } = false;
	}
}