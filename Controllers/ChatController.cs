using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace R3AIA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new();

        private const string SystemPrompt = @"أنت ""رعاية"" المساعد الذكي لمنصة رعاية الخيرية الطبية في مصر.

معلومات المنصة:
- منصة خيرية تقدم رعاية صحية مجانية للمحتاجين في مصر
- الخدمات: استشارات طبية مجانية، طلب أدوية بالروشتة، توصيل أدوية، أطباء بأسعار مخفضة، تبرعات، نداءات طوارئ
- المريض: يطلب استشارة من ""استشاراتي""، يطلب دواء من ""طلبات الأدوية"" برفع روشتة، يتبرع من ""التبرعات""
- الطبيب: يرى الطلبات في محافظته، يقبل ويحدد مواعيد، يسجل اكتمال الكشف
- الصيدلي: يرى طلبات الأدوية، يقبل ما يستطيع توفيره
- المتطوع: يأخذ مهام توصيل، يستلم من الصيدلية ويوصل للمريض
- التواصل: 01097972975، info@r3aya.org، القاهرة

قواعد مهمة جداً:
- رد بالعربية فقط بجملة أو جملتين كحد أقصى
- لا تكتب تفكيرك أو تحليلك أو أي شيء بالإنجليزية
- لا تكرر السؤال ولا تكتب مقدمات
- رد مباشرة على السؤال بإجابة قصيرة";

        private static readonly string[] FreeModels = new[]
        {
            "google/gemma-4-26b-a4b-it:free",
            "google/gemma-4-31b-it:free",
            "qwen/qwen3-14b:free",
            "qwen/qwen3-30b-a3b:free",
            "meta-llama/llama-4-scout:free",
            "meta-llama/llama-4-maverick:free",
            "deepseek/deepseek-r1-0528:free",
            "minimax/minimax-m2.5:free"
        };

        public ChatController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("الرسالة فارغة");

            var apiKey = _configuration["OpenRouter:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "خدمة الدردشة غير مُهيأة");

            // Build messages array
            var systemContent = SystemPrompt + $"\nدور المستخدم: {request.Role ?? "مستخدم"}";
            if (!string.IsNullOrEmpty(request.UserName))
                systemContent += $"\nاسم المستخدم: {request.UserName}. ناديه باسمه.";

            var messages = new List<object>
            {
                new { role = "system", content = systemContent }
            };

            // Add history (last 6 messages only)
            if (request.History != null && request.History.Count > 0)
            {
                var recent = request.History.TakeLast(6);
                foreach (var msg in recent)
                {
                    if (msg.Type == "user")
                        messages.Add(new { role = "user", content = msg.Text ?? "" });
                    else if (msg.Type == "bot" && !string.IsNullOrEmpty(msg.Text))
                        messages.Add(new { role = "assistant", content = msg.Text });
                }
            }

            messages.Add(new { role = "user", content = request.Message });

            // Try models with fallback (max 3 attempts to avoid burning rate limits)
            var modelsToTry = FreeModels.Take(3).ToArray();
            for (int i = 0; i < modelsToTry.Length; i++)
            {
                var model = modelsToTry[i];
                try
                {
                    var result = await CallModel(model, messages, apiKey);
                    if (result != null)
                    {
                        var cleaned = CleanResponse(result);
                        if (!string.IsNullOrEmpty(cleaned))
                            return Ok(new { reply = cleaned });
                    }
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                {
                    Console.WriteLine($"Chat rate limited on {model}. Waiting before retry...");
                    await Task.Delay(2000); // Wait 2 seconds on rate limit
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chat model {model} error: {ex.Message}");
                }

                // Small delay between model attempts to avoid rate limit cascade
                if (i < modelsToTry.Length - 1)
                    await Task.Delay(500);
            }

            return StatusCode(503, new { error = "الخدمة غير متاحة حالياً، حاول لاحقاً" });
        }

        private async Task<string?> CallModel(string model, List<object> messages, string apiKey)
        {
            var requestBody = new
            {
                model,
                messages,
                temperature = 0.7,
                max_tokens = 200
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Headers.Add("HTTP-Referer", "https://r3aya.vercel.app");
            httpRequest.Headers.Add("X-Title", "R3aya AI");

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"OpenRouter {model}: {(int)response.StatusCode} - {errText[..Math.Min(80, errText.Length)]}");

                if ((int)response.StatusCode == 401 || (int)response.StatusCode == 403)
                    throw new UnauthorizedAccessException("Invalid API key");

                if ((int)response.StatusCode == 429)
                    throw new HttpRequestException("429 Rate Limited");

                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return text;
        }

        private static string CleanResponse(string text)
        {
            var cleaned = text;

            // Remove thinking tags
            cleaned = Regex.Replace(cleaned, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"<thinking>[\s\S]*?</thinking>", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\[INST\][\s\S]*?\[/INST\]", "", RegexOptions.IgnoreCase);
            cleaned = cleaned.Trim();

            // If response contains English reasoning before Arabic, extract only Arabic part
            var arabicMatch = Regex.Match(cleaned, @"[\u0600-\u06FF]");
            if (arabicMatch.Success && arabicMatch.Index > 50)
            {
                cleaned = cleaned[arabicMatch.Index..].Trim();
            }

            return cleaned;
        }
    }

    // ── DTOs ──
    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? Role { get; set; }
        public string? UserName { get; set; }
        public List<ChatMessageDto>? History { get; set; }
    }

    public class ChatMessageDto
    {
        public string Type { get; set; } = "";
        public string? Text { get; set; }
    }
}
