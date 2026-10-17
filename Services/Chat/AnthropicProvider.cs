using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VIUUniversity.Services.Chat
{
    /// <summary>
    /// Anthropic Messages API (Claude).
    /// The system prompt is passed in the dedicated "system" field rather than as a turn.
    /// </summary>
    public class AnthropicProvider : IChatProvider
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly ChatOptions _options;
        private readonly ILogger<AnthropicProvider> _log;

        public AnthropicProvider(
            IHttpClientFactory factory,
            IOptions<ChatOptions> options,
            ILogger<AnthropicProvider> log)
        {
            _options = options.Value;
            _http = factory.CreateClient("anthropic");
            _log = log;
        }

        public string Name => "anthropic";

        public async Task<string?> CompleteAsync(IReadOnlyList<ChatTurn> turns, CancellationToken ct)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                ? "https://api.anthropic.com/v1"
                : _options.BaseUrl.TrimEnd('/');

            var system = turns.FirstOrDefault(t => t.Role == "system")?.Content;
            var messages = turns
                .Where(t => t.Role != "system")
                .Select(t => new { role = t.Role, content = t.Content })
                .ToList();

            if (messages.Count == 0)
                return null;

            var payload = new Dictionary<string, object?>
            {
                ["model"] = _options.Model,
                ["max_tokens"] = _options.MaxTokens,
                ["system"] = system,
                ["messages"] = messages
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/messages");
                request.Headers.Add("x-api-key", _options.ApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning("AI provider {Provider} returned {Status}", Name, (int)response.StatusCode);
                    return null;
                }

                using var document = JsonDocument.Parse(body);
                if (!document.RootElement.TryGetProperty("content", out var content))
                    return null;

                // Content is a block array; concatenate the text blocks.
                var sb = new StringBuilder();
                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var type)
                        && type.GetString() == "text"
                        && block.TryGetProperty("text", out var text))
                    {
                        sb.Append(text.GetString());
                    }
                }

                var result = sb.ToString().Trim();
                return string.IsNullOrEmpty(result) ? null : result;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _log.LogWarning("AI provider {Provider} timed out after {Seconds}s",
                    Name, _options.TimeoutSeconds);
                return null;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "AI provider {Provider} failed", Name);
                return null;
            }
        }
    }
}