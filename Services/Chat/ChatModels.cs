namespace VIUUniversity.Services.Chat
{
    /// <summary>One turn of a conversation, as sent to / received from the model.</summary>
    public record ChatTurn(string Role, string Content);

    /// <summary>
    /// Verdict on where an answer came from. The UI must be able to tell the visitor
    /// whether they are reading a fact held in the school's database or a model's
    /// general reply, because the school is accountable for the former.
    /// </summary>
    public enum AnswerSource
    {
        /// <summary>Answered from a fact retrieved from this system's own database.</summary>
        Grounded,

        /// <summary>Model answered from its own general knowledge - not a school fact.</summary>
        General,

        /// <summary>No verified data; the model declined or the provider failed.</summary>
        Unavailable
    }

    public class ChatAnswer
    {
        public required string Reply { get; init; }
        public required AnswerSource Source { get; init; }

        /// <summary>Short note shown under the answer, e.g. "Nguồn: dữ liệu trường".</summary>
        public string? SourceNote { get; init; }

        /// <summary>Suggested follow-up questions.</summary>
        public List<string> Suggestions { get; init; } = new();

        /// <summary>Deep links offered alongside the answer.</summary>
        public List<ChatLink> Links { get; init; } = new();
    }

    public record ChatLink(string Label, string Url);

    /// <summary>Configuration for the chat assistant. Bound from appsettings / env / user-secrets.</summary>
    public class ChatOptions
    {
        public const string SectionName = "Chat";

        /// <summary>openai | anthropic | none</summary>
        public string Provider { get; set; } = "none";

        /// <summary>
        /// API key. Chỉ dùng làm phương án dự phòng — thứ tự ưu tiên là biến môi
        /// trường OPENAI_API_KEY (yêu cầu bảo mật), sau đó mới đến giá trị này.
        /// Không được commit giá trị thật vào appsettings.json.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Base URL, e.g. https://api.openai.com/v1 (OpenAI) or https://api.anthropic.com</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Tên model của Responses API. Mặc định gpt-4o-mini (rẻ, nhanh, đủ cho
        /// trợ lý tuyển sinh). Ghi đè bằng Chat__Model khi muốn dùng model khác.
        /// </summary>
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>Allow the model to answer outside the school's dataset. Off = strict facts only.</summary>
        public bool AllowGeneralKnowledge { get; set; }

        public int MaxTokens { get; set; } = 700;

        /// <summary>Request timeout. Also the budget we expose to the visitor as "đang suy nghĩ".</summary>
        public int TimeoutSeconds { get; set; } = 25;

        /// <summary>
        /// Provider OpenAI chấp nhận key từ biến môi trường, nên phải kiểm tra
        /// cả hai nguồn — nếu chỉ nhìn ApiKey thì sẽ báo "chưa cấu hình" sai.
        /// </summary>
        public bool IsConfigured
        {
            get
            {
                if (Provider.Equals("none", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (string.IsNullOrWhiteSpace(Model))
                    return false;

                if (Provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
                {
                    return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                        || !string.IsNullOrWhiteSpace(ApiKey);
                }

                return !string.IsNullOrWhiteSpace(ApiKey);
            }
        }
    }
}