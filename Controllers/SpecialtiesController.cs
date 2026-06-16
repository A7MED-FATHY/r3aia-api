using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;

namespace R3AIA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecialtiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SpecialtiesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// جلب قائمة جميع التخصصات الطبية من قاعدة البيانات.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSpecialties()
        {
            var specialties = await _context.Specialties
                .Select(s => new { s.Id, s.Name })
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Ok(specialties);
        }
    }
}
