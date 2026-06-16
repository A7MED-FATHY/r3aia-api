using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R3AIA.Services;

namespace R3AIA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;

        public UploadController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { status = "error", message = "لم يتم إرسال صورة." });
            }

            // Max 5 MB
            if (image.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { status = "error", message = "حجم الصورة يجب ألا يتجاوز 5 ميجابايت." });
            }

            try
            {
                var result = await _cloudinaryService.UploadImageAsync(image);

                return Ok(new
                {
                    url = result.Url,
                    publicId = result.PublicId,
                    status = "success"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
