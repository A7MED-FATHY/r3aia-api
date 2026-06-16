using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using System.Security.Claims;
using static R3AIA.Models.Enums;

namespace R3AIA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PremiumController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PremiumController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("order-box")]
        public async Task<IActionResult> OrderR3aiaBox([FromBody] OrderBoxRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserType != UserType.Premium)
            {
                return StatusCode(403, new { message = "هذه الخدمة متاحة فقط لأصحاب الحسابات البريميم." });
            }

            var settings = await _context.R3aiaBoxSettings.FirstOrDefaultAsync();
            if (settings == null || !settings.IsActive)
            {
                return BadRequest(new { message = "جهاز رعاية بوكس غير متاح حالياً." });
            }

            if (settings.AvailableQuantity <= 0)
            {
                return BadRequest(new { message = "عفواً، لقد نفدت الكمية المتاحة." });
            }

            // Create Order
            var order = new R3aiaBoxOrder
            {
                UserId = userId,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Price = settings.Price,
                Status = "pending",
                OrderDate = DateTime.Now
            };

            // Deduct inventory
            settings.AvailableQuantity -= 1;

            _context.R3aiaBoxOrders.Add(order);
            
            // Add notification for Admin
            var adminUsers = await _context.Users.Where(u => u.UserType == UserType.Admin).ToListAsync();
            foreach (var admin in adminUsers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Title = "طلب جديد - رعاية بوكس",
                    Message = $"تم استلام طلب جديد من {request.FullName} برقم هاتف {request.PhoneNumber}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "تم تسجيل طلبك بنجاح", 
                orderId = order.Id,
                whatsAppOrderNumber = settings.WhatsAppOrderNumber
            });
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var orders = await _context.R3aiaBoxOrders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new {
                    o.Id,
                    o.Status,
                    o.Price,
                    o.OrderDate,
                    o.FullName,
                    o.Address,
                    o.PhoneNumber
                })
                .ToListAsync();

            return Ok(orders);
        }
    }

    public class OrderBoxRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
