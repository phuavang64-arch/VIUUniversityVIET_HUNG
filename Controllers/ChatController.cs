using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VIUUniversity.Services.Chat;

namespace VIUUniversity.Controllers
{
    /// <summary>
    /// Chat endpoint for the admission assistant.
    ///
    /// POST rather than GET: the question is unbounded user text, conversation history
    /// travels in the body, and GETs get logged/cached in ways that suit neither.
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/chat")]
    [EnableRateLimiting(RateLimitPolicies.Chat)]
    public class ChatController : ControllerBase
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // "Unavailable" phải ra chuỗi, không phải 0 — JS so sánh theo tên.
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IChatService _chat;
        private readonly ILogger<ChatController> _log;

        public ChatController(IChatService chat, ILogger<ChatController> log)
        {
            _chat = chat;
            _log = log;
        }

        /// <summary>Front-end uses this to show the correct status badge before sending.</summary>
        [HttpGet("status")]
        public IActionResult Status() => Ok(new
        {
            configured = _chat.IsConfigured,
            endpoint = _chat.Endpoint
        });

        public class ChatRequest
        {
            public string? Message { get; set; }
            public List<ChatTurn>? History { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request, CancellationToken ct)
        {
            if (request?.Message is null)
                return BadRequest(new { error = "Thiếu nội dung câu hỏi." });

            try
            {
                var answer = await _chat.AskAsync(request.Message, request.History ?? new List<ChatTurn>(), ct);
                return Ok(JsonSerializer.SerializeToElement(answer, Json));
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    reply = "<p>Yêu cầu mất quá lâu. Bạn vui lòng thử lại.</p>",
                    source = AnswerSource.Unavailable.ToString()
                });
            }
            catch (Exception ex)
            {
                // Log the detail server-side; return something the visitor can act on.
                _log.LogError(ex, "Chat request failed");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    reply = "<p>Hệ thống trợ lý đang gặp sự cố. Bạn vui lòng thử lại sau " +
                            "hoặc liên hệ hotline tuyển sinh.</p>",
                    source = AnswerSource.Unavailable.ToString()
                });
            }
        }
    }
}