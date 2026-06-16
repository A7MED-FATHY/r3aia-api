using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R3AIA.DTOs;

public class CreateMedicalRequestDto
{
	[Required]
	public int SpecialtyId { get; set; }

	[Required]
	public string Description { get; set; } = string.Empty;

	public bool ChronicDisease { get; set; }
	
	/// <summary>
	/// صور الأشعة أو التحاليل (اختياري، حد أقصى 5 صور)
	/// </summary>
	public List<IFormFile>? MedicalImages { get; set; }
}

