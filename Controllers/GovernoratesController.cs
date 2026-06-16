using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models; 

namespace R3AIA.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class GovernoratesController : ControllerBase
	{
		private readonly AppDbContext _context;

		public GovernoratesController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> GetGovernorates()
		{
			var governorates = await _context.Governorates
				.Select(g => new { g.Id, g.Name })
				.ToListAsync();

			return Ok(governorates);
		}

		[HttpGet("{id}/cities")]
		public async Task<IActionResult> GetCities(int id)
		{
			var cities = await _context.Cities
				.Where(c => c.GovernorateId == id)
				.Select(c => new { c.Id, c.Name })
				.ToListAsync();

			return Ok(cities);
		}
	}
}
