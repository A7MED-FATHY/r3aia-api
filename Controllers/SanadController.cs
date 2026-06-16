using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using R3AIA.Models;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient,Premium")]
public class SanadController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public SanadController(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;

    // ─── التحقق من أهلية المريض لاستخدام سند ─────────────────────
    private async Task<(bool ok, IActionResult? error, ApplicationUser? user)> CheckPremiumAsync()
    {
        var userId = GetUserId();
        if (userId == null) return (false, Unauthorized(), null);

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return (false, NotFound(), null);

        if (!user.IsPremium || (user.SubscriptionEndDate.HasValue && user.SubscriptionEndDate.Value < DateTime.Now))
            return (false, Forbid(), null);

        return (true, null, user);
    }

    // ─── حفظ / تحديث إعدادات سند ─────────────────────────────────
    [HttpPost("setup")]
    public async Task<IActionResult> SaveSetup([FromBody] SaveSanadSetupDto dto)
    {
        var (ok, error, user) = await CheckPremiumAsync();
        if (!ok) return error!;

        var userId = GetUserId()!;

        var existing = await _context.SanadSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (existing == null)
        {
            existing = new SanadSetting { UserId = userId };
            _context.SanadSettings.Add(existing);
        }

        existing.EmergencyContactName = dto.EmergencyContactName;
        existing.EmergencyContactPhone = dto.EmergencyContactPhone;
        existing.RelationshipType = dto.RelationshipType;
        existing.AlertTimesJson = dto.AlertTimesJson;
        existing.DelaySeconds = dto.DelaySeconds;
        existing.IsActive = true;
        existing.UpdatedAt = DateTime.Now;

        // ربط حساب الحارس إن تم تسجيله
        if (!string.IsNullOrEmpty(dto.CompanionUserId))
        {
            var companion = await _context.Users.FindAsync(dto.CompanionUserId);
            if (companion != null) existing.CompanionUserId = dto.CompanionUserId;
        }

        // تحديث علامة إكمال الإعداد في الـ User
        user!.HasSanadSetup = true;

        await _context.SaveChangesAsync();

        return Ok(new { message = "تم حفظ إعدادات سند بنجاح." });
    }

    // ─── جلب الإعدادات الحالية ────────────────────────────────────
    [HttpGet("setup")]
    public async Task<IActionResult> GetSetup()
    {
        var (ok, error, _) = await CheckPremiumAsync();
        if (!ok) return error!;

        var userId = GetUserId()!;
        var setting = await _context.SanadSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (setting == null) return Ok(null);

        return Ok(new
        {
            emergencyContactName = setting.EmergencyContactName,
            emergencyContactPhone = setting.EmergencyContactPhone,
            relationshipType = setting.RelationshipType,
            alertTimesJson = setting.AlertTimesJson,
            delaySeconds = setting.DelaySeconds,
            isActive = setting.IsActive,
            updatedAt = setting.UpdatedAt,
            companionUserId = setting.CompanionUserId,
            hasCompanionAccount = !string.IsNullOrEmpty(setting.CompanionUserId)
        });
    }

    // ─── تسجيل "أنا بخير" ──────────────────────────────────────
    [HttpPost("response")]
    public async Task<IActionResult> LogResponse()
    {
        var (ok, error, user) = await CheckPremiumAsync();
        if (!ok) return error!;

        var userId = GetUserId()!;

        // البحث عن آخر سجل لم يستلم استجابة بعد
        var recentLog = await _context.SanadLogs
            .Where(l => l.UserId == userId && !l.ResponseReceived)
            .OrderByDescending(l => l.TriggerTime)
            .FirstOrDefaultAsync();

        if (recentLog != null)
        {
            recentLog.ResponseReceived = true;
            recentLog.ResponseTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        else
        {
            // إنشاء سجل جديد للاستجابة
            _context.SanadLogs.Add(new SanadLog
            {
                UserId = userId,
                TriggerTime = DateTime.Now,
                ResponseReceived = true,
                ResponseTime = DateTime.Now,
                EmergencyActivated = false
            });
            await _context.SaveChangesAsync();
        }

        // إشعار الحارس (جهة الاتصال) إذا كان متصلاً
        var setting = await _context.SanadSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (setting?.CompanionUserId != null)
        {
            await _notificationService.SendToUserAsync(
                setting.CompanionUserId,
                "✅ اطمئنان",
                $"قام {user!.FullName} بالاستجابة لتنبيه سند وهو الآن بخير! 💚"
            );
        }

        return Ok(new { message = "تم تسجيل استجابتك. أنت بخير! 💚" });
    }

    // ─── تفعيل سيناريو الطوارئ ────────────────────────────────────
    [HttpPost("emergency")]
    public async Task<IActionResult> TriggerEmergency([FromBody] EmergencyTriggerDto dto)
    {
        var (ok, error, user) = await CheckPremiumAsync();
        if (!ok) return error!;

        var userId = GetUserId()!;

        // تسجيل حدث الطوارئ
        var log = new SanadLog
        {
            UserId = userId,
            TriggerTime = DateTime.Now,
            ResponseReceived = false,
            EmergencyActivated = true,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };
        _context.SanadLogs.Add(log);
        await _context.SaveChangesAsync();

        // إشعار الأدمن
        await _notificationService.SendToRoleAsync(
            Enums.UserType.Admin,
            "🚨 تنبيه طوارئ سند!",
            $"المستخدم {user!.FullName} لم يستجب للتنبيه. الموقع: {dto.Latitude}, {dto.Longitude}"
        );

        // إشعار الحارس (جهة الاتصال) مباشرةً عبر حسابه في التطبيق
        var setting = await _context.SanadSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (setting?.CompanionUserId != null)
        {
            var locationText = (dto.Latitude.HasValue && dto.Longitude.HasValue)
                ? $"https://maps.google.com/?q={dto.Latitude},{dto.Longitude}"
                : "غير متاح";

            await _notificationService.SendToUserAsync(
                setting.CompanionUserId,
                $"🚨 طوارئ - {user!.FullName} يحتاج مساعدة!",
                $"لم يستجب {user.FullName} لتنبيه سند، يرجى التواصل معه فوراً!\n\nموقعه:\n{locationText}"
            );
        }

        // إشعار تأكيد للمريض
        await _notificationService.SendToUserAsync(
            userId,
            "🚨 تم تفعيل سند",
            "تم تفعيل بروتوكول الطوارئ. تم إرسال موقعك وإشعار جهة الاتصال."
        );

        return Ok(new { message = "تم تفعيل بروتوكول الطوارئ وإرسال إشعارات المساعدة.", logId = log.Id });
    }

    // ─── جلب سجل أحداث سند الخاص بالمريض ────────────────────────
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (ok, error, _) = await CheckPremiumAsync();
        if (!ok) return error!;

        var userId = GetUserId()!;

        var logs = await _context.SanadLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.TriggerTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.TriggerTime,
                l.ResponseReceived,
                l.ResponseTime,
                l.EmergencyActivated,
                l.Latitude,
                l.Longitude
            })
            .ToListAsync();

        return Ok(logs);
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────
public record SaveSanadSetupDto(
    string EmergencyContactName,
    string EmergencyContactPhone,
    string? RelationshipType,
    string AlertTimesJson,
    int DelaySeconds,
    string? CompanionUserId = null
);

public record EmergencyTriggerDto(double? Latitude, double? Longitude);
