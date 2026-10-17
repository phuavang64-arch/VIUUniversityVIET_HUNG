using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace VIUUniversity.Services.Chat
{
    /// <summary>
    /// Works with OpenAI and any OpenAI-compatible endpoint (Azure OpenAI, OpenRouter,
    /// Together, a local vLLM/Ollama gateway...). Only the base URL differs.
    ///
    /// The API key is read from server-side configuration and is never sent to the browser.
    /// </summary>
    public class OpenAiCompatibleProvider : IChatProvider
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _http;
        private readonly ChatOptions _options;
        private readonly ILogger<OpenAiCompatibleProvider> _log;

        public OpenAiCompatibleProvider(
            IHttpClientFactory factory,
            IOptions<ChatOptions> options,
            ILogger<OpenAiCompatibleProvider> log)
        {
            _options = options.Value;
            _http = factory.CreateClient("openai-compatible");
            _log = log;
        }

        public string Name => "openai";

        public async Task<string?> CompleteAsync(IReadOnlyList<ChatTurn> turns, CancellationToken ct)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                ? "https://api.openai.com/v1"
                : _options.BaseUrl.TrimEnd('/');

            var payload = new
            {
                model = _options.Model,
                max_tokens = _options.MaxTokens,
                temperature = 0.2,
                messages = turns.Select(t => new
                {
                    role = t.Role,
                    content = t.Content
                })
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                request.Content = JsonContent.Create(payload, options: Json);

                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    // Never surface the raw provider body: it can echo the request back.
                    _log.LogWarning("AI provider {Provider} returned {Status}", Name, (int)response.StatusCode);
                    return null;
                }

                using var document = JsonDocument.Parse(body);
                var choices = document.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                    return null;

                var message = choices[0].GetProperty("message");
                return message.GetProperty("content").GetString();
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