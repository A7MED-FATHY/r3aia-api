using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models; 

namespace R3AIA.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CitiesController : ControllerBase
	{
		// استخدام الاسم الصحيح الموجود في مجلد Models
		private readonly AppDbContext _context;

		public CitiesController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("by-governorate/{govId}")]
		public async Task<IActionResult> GetCities(int govId)
		{
			var cities = await _context.Cities
				.Where(c => c.GovernorateId == govId)
				.ToListAsync();

			return Ok(cities);
		}
	}
}