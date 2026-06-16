using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using R3AIA.Models;
using R3AIA.Services;
using static R3AIA.Models.Enums;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;

    public SubscriptionController(AppDbContext context, IFileService fileService, INotificationService notificationService)
    {
        _context = context;
        _fileService = fileService;
        _notificationService = notificationService;
    }

    // ─── جلب حالة الاشتراك الحالية ───────────────────────────────
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // انتهاء الاشتراك؟ نحدّث تلقائياً
        if (user.IsPremium && user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value < DateTime.Now)
        {
            user.IsPremium = false;
            user.HasSanadSetup = false;
            await _context.SaveChangesAsync();
        }

        // آخر طلب معلق
        var pendingRequest = await _context.SubscriptionRequests
            .Where(r => r.UserId == userId && r.Status == SubscriptionRequestStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            isPremium = user.IsPremium,
            hasSanadSetup = user.HasSanadSetup,
            subscriptionEndDate = user.SubscriptionEndDate,
            isExpired = user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value < DateTime.Now,
            hasPendingRequest = pendingRequest != null,
            pendingRequestId = pendingRequest?.Id
        });
    }

    // ─── تقديم طلب اشتراك (دفع يدوي) ─────────────────────────────
    [HttpPost("request")]
    [Authorize(Roles = "Premium")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> RequestSubscription([FromForm] string paymentMethod, IFormFile? screenshot)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.IsPremium) return BadRequest(new { message = "أنت مشترك بالفعل في الباقة المدفوعة." });

        // منع التكرار – إذا كان هناك طلب معلق بالفعل
        var alreadyPending = await _context.SubscriptionRequests
            .AnyAsync(r => r.UserId == userId && r.Status == SubscriptionRequestStatus.Pending);
        if (alreadyPending) return BadRequest(new { message = "لديك طلب اشتراك قيد المراجعة بالفعل." });

        if (!Enum.TryParse<SubscriptionPaymentMethod>(paymentMethod, true, out var method))
            return BadRequest(new { message = "وسيلة دفع غير صالحة." });

        string? screenshotPath = null;
        if (screenshot != null)
            screenshotPath = await _fileService.SaveImageAsync(screenshot, "Uploads/Subscriptions");

        var request = new SubscriptionRequest
        {
            UserId = userId,
            PaymentMethod = method,
            ScreenshotPath = screenshotPath,
            Status = SubscriptionRequestStatus.Pending,
        };

        _context.SubscriptionRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تم إرسال طلب الاشتراك بنجاح. سيتم مراجعته من قِبل الإدارة.", requestId = request.Id });
    }

    // ─── [Admin] جلب جميع طلبات الاشتراك ────────────────────────
    [HttpGet("requests")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status)
    {
        var query = _context.SubscriptionRequests
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<SubscriptionRequestStatus>(status, true, out var statusEnum))
            query = query.Where(r => r.Status == statusEnum);

        var requests = await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                userName = r.User.FullName,
                userEmail = r.User.Email,
                paymentMethod = r.PaymentMethod.ToString(),
                r.ScreenshotPath,
                status = r.Status.ToString(),
                r.RequestedAt,
                r.ReviewedAt,
                r.RejectionNotes
            })
            .ToListAsync();

        return Ok(requests);
    }

    // ─── [Admin] قبول طلب الاشتراك ────────────────────────────────
    [HttpPost("approve/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveRequest(int id, [FromQuery] int durationDays = 30)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

        var request = await _context.SubscriptionRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound(new { message = "الطلب غير موجود." });
        if (request.Status != SubscriptionRequestStatus.Pending)
            return BadRequest(new { message = "الطلب تمت مراجعته بالفعل." });

        // تفعيل الـ Premium
        request.Status = SubscriptionRequestStatus.Approved;
        request.ReviewedAt = DateTime.Now;
        request.ReviewedByAdminId = adminId;

        request.User.IsPremium = true;
        request.User.SubscriptionEndDate = DateTime.Now.AddDays(durationDays);

        await _context.SaveChangesAsync();

        // إرسال إشعار للمستخدم
        await _notificationService.SendToUserAsync(
            request.UserId,
            "🎉 تم تفعيل خدمة سند!",
            "تهانينا! تم قبول طلب اشتراكك في خدمة سند. يمكنك الآن إعداد الخدمة من قائمتك الرئيسية."
        );

        return Ok(new { message = "تم قبول طلب الاشتراك وتفعيل الباقة المدفوعة." });
    }

    // ─── [Admin] رفض طلب الاشتراك ─────────────────────────────────
    [HttpPost("reject/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectSubscriptionDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

        var request = await _context.SubscriptionRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound(new { message = "الطلب غير موجود." });
        if (request.Status != SubscriptionRequestStatus.Pending)
            return BadRequest(new { message = "الطلب تمت مراجعته بالفعل." });

        request.Status = SubscriptionRequestStatus.Rejected;
        request.ReviewedAt = DateTime.Now;
        request.ReviewedByAdminId = adminId;
        request.RejectionNotes = dto.Reason;

        await _context.SaveChangesAsync();

        await _notificationService.SendToUserAsync(
            request.UserId,
            "❌ طلب الاشتراك مرفوض",
            $"تم رفض طلب اشتراكك في خدمة سند. السبب: {dto.Reason ?? "لم يتم تحديد سبب."}. يمكنك إعادة التقديم."
        );

        return Ok(new { message = "تم رفض طلب الاشتراك." });
    }
}

public record RejectSubscriptionDto(string? Reason);
