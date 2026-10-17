using Microsoft.Extensions.Options;

namespace VIUUniversity.Services.Chat
{
    public interface IChatService
    {
        bool IsConfigured { get; }

        /// <summary>Absolute endpoint the browser should call.</summary>
        string Endpoint { get; }

        Task<ChatAnswer> AskAsync(string question, IReadOnlyList<ChatTurn> history, CancellationToken ct);
    }

    /// <summary>
    /// Orchestrates retrieval + model call. The prompt tells the model to answer only from
    /// the supplied facts when facts are present, and to say so plainly when they are not,
    /// rather than inventing tuition fees or admission scores.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IChatProvider _provider;
        private readonly KnowledgeService _knowledge;
        private readonly ChatOptions _options;
        private readonly ILogger<ChatService> _log;

        public ChatService(
            IChatProvider provider,
            KnowledgeService knowledge,
            IOptions<ChatOptions> options,
            ILogger<ChatService> log)
        {
            _provider = provider;
            _knowledge = knowledge;
            _options = options.Value;
            _log = log;
        }

        public bool IsConfigured => _options.IsConfigured;

        public string Endpoint => "/api/chat";

        private string SystemPrompt(bool hasFacts) => $"""
            Bạn là trợ lý AI trên website Trường Đại học Công nghiệp Việt – Hung.
            Hãy giao tiếp bằng tiếng Việt rõ ràng, lịch sự và dễ hiểu. Ưu tiên hỗ trợ các câu
            hỏi về ngành học, tuyển sinh, chương trình đào tạo, học phí, học bổng, cơ sở đào tạo,
            thông báo, sự kiện và thông tin liên hệ của trường.

            NGUYÊN TẮC BẮT BUỘC:
            1. Chỉ khẳng định thông tin về trường khi có căn cứ từ DỮ LIỆU TRƯỜNG được cung cấp
               bên dưới. Nếu chưa có dữ liệu hoặc không chắc chắn, hãy nói rõ và hướng dẫn người
               dùng liên hệ bộ phận phụ trách.
            2. {(_options.AllowGeneralKnowledge
                    ? "Khi người dùng hỏi ngoài phạm vi dữ liệu trường, có thể trả lời ngắn gọn nếu phù hợp, sau đó hướng dẫn quay lại nội dung liên quan đến Việt – Hung."
                    : "KHÔNG trả lời ngoài phạm vi dữ liệu trường; hãy hướng người dùng về bộ phận phụ trách.")}
            3. TUYỆT ĐỐI không tự tạo điểm chuẩn, học phí, hạn nộp hồ sơ, chỉ tiêu, số liệu hay
               chính sách. Thiếu dữ liệu thì nói là chưa có dữ liệu.
            4. Nếu một trường trong dữ liệu ghi "chưa cập nhật"/"chưa công bố", phải giữ nguyên
               nghĩa đó, không suy đoán.
            5. Không bao giờ yêu cầu người dùng cung cấp mật khẩu, API key hoặc thông tin nhạy cảm
               không cần thiết.
            6. Trả lời ngắn gọn, tối đa 5 câu.
            7. KHÔNG dùng Markdown. Dùng HTML tối thiểu: <p>, <ul><li>, <strong>, <a href="...">.
            8. {(hasFacts
                    ? "Dữ liệu trường đã được cung cấp bên dưới cho câu hỏi này."
                    : "KHÔNG có dữ liệu trường nào liên quan tới câu hỏi này — hãy nói rõ điều đó.")}
            """;

        public async Task<ChatAnswer> AskAsync(
            string question,
            IReadOnlyList<ChatTurn> history,
            CancellationToken ct)
        {
            question = (question ?? string.Empty).Trim();
            if (question.Length == 0)
            {
                return Unavailable("Bạn vui lòng nhập câu hỏi.");
            }

            if (question.Length > 500)
                question = question[..500];

            var knowledge = await _knowledge.BuildAsync(question, ct);
            var hasFacts = knowledge.HasTopicFacts;

            if (!IsConfigured)
            {
                // Không có API key: vẫn trả lời được phần có dữ liệu, và nói rõ phần còn lại.
                return hasFacts
                    ? new ChatAnswer
                    {
                        Reply = "<p>Trợ lý AI chưa được cấu hình nên mình mới chỉ tra cứu được dữ liệu " +
                                "trực tiếp từ hệ thống.</p>",
                        Source = AnswerSource.Unavailable,
                        SourceNote = "Chưa cấu hình dịch vụ AI",
                        Links = knowledge.Links,
                        Suggestions = knowledge.FollowUps
                    }
                    : Unavailable(
                        "Trợ lý AI chưa được cấu hình và mình chưa tìm thấy dữ liệu phù hợp trong hệ thống. " +
                        "Bạn vui lòng liên hệ hotline tuyển sinh để được hỗ trợ trực tiếp.");
            }

            var turns = new List<ChatTurn> { new("system", SystemPrompt(hasFacts)) };

            // Giữ lịch sử gần đây để hiểu câu hỏi tiếp nối ("vậy còn cái đó?").
            foreach (var turn in history.TakeLast(6))
            {
                if (turn.Role is "user" or "assistant")
                    turns.Add(new ChatTurn(turn.Role, turn.Content[..Math.Min(turn.Content.Length, 800)]));
            }

            turns.Add(new ChatTurn("user", question));

            var prompt = BuildUserTurn(question, knowledge.Text);
            turns[^1] = new ChatTurn("user", prompt);

            var reply = await _provider.CompleteAsync(turns, ct);

            if (string.IsNullOrWhiteSpace(reply))
            {
                _log.LogWarning("Assistant returned no content for provider {Provider}", _provider.Name);

                return hasFacts
                    ? new ChatAnswer
                    {
                        Reply = "<p>Dịch vụ AI không phản hồi. Bạn có thể xem các thông tin bên dưới " +
                                "hoặc liên hệ hotline tuyển sinh.</p>",
                        Source = AnswerSource.Unavailable,
                        SourceNote = "Dịch vụ AI tạm thời không phản hồi",
                        Links = knowledge.Links,
                        Suggestions = knowledge.FollowUps
                    }
                    : Unavailable(
                        "Dịch vụ AI tạm thời không phản hồi. Bạn vui lòng thử lại sau hoặc liên hệ " +
                        "hotline tuyển sinh để được hỗ trợ trực tiếp.");
            }

            return new ChatAnswer
            {
                Reply = Sanitize(reply!),
                Source = hasFacts ? AnswerSource.Grounded : AnswerSource.General,
                SourceNote = hasFacts
                    ? "Câu trả lời dựa trên dữ liệu đang lưu trong hệ thống của trường."
                    : "Nội dung này do AI tổng hợp, không phải thông tin chính thức của trường.",
                Links = knowledge.Links,
                Suggestions = knowledge.FollowUps
            };
        }

        private static string BuildUserTurn(string question, string knowledge) =>
            $"CÂU HỎI: {question}\n\nDỮ LIỆU TRƯỜNG:\n{knowledge}";

        private static ChatAnswer Unavailable(string message) => new()
        {
            Reply = $"<p>{message}</p>",
            Source = AnswerSource.Unavailable,
            SourceNote = "Chưa có đủ dữ liệu để trả lời",
            Links = new List<ChatLink> { new("Liên hệ nhà trường", "/lien-he") },
            Suggestions = new List<string>
            {
                "Điểm chuẩn tuyển sinh năm 2025 là bao nhiêu?",
                "Trường đang đào tạo những ngành nào?",
                "Học phí và phương thức xét tuyển thế nào?"
            }
        };

        /// <summary>
        /// The model is instructed to emit minimal HTML, but a model is still untrusted
        /// input: strip anything active before it reaches innerHTML in the browser.
        /// </summary>
        private static string Sanitize(string html)
        {
            var sanitizer = new HtmlSanitizerAdapter();
            return sanitizer.Sanitize(html);
        }
    }

    /// <summary>Thin adapter over the shared HtmlSanitizer so ChatService has no direct dependency.</summary>
    internal sealed class HtmlSanitizerAdapter
    {
        private readonly Ganss.Xss.HtmlSanitizer _sanitizer = Create();

        private static Ganss.Xss.HtmlSanitizer Create()
        {
            var s = new Ganss.Xss.HtmlSanitizer();
            s.AllowedTags.Add("a");
            s.AllowedTags.Add("ul");
            s.AllowedTags.Add("ol");
            s.AllowedTags.Add("li");
            s.AllowedTags.Add("strong");
            s.AllowedTags.Add("em");
            s.AllowedTags.Add("br");
            s.AllowedAttributes.Add("href");
            return s;
        }

        public string Sanitize(string html) => _sanitizer.Sanitize(html);
    }
}