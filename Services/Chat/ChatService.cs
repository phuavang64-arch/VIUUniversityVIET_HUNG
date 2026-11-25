using Markdig;
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
    /// Intelligent, conversational AI service that orchestrates database grounding + LLM completion.
    /// Provides an open, friendly ChatGPT-like experience while remaining deeply accurate about university facts.
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

        private static string SystemPrompt => """
            Bạn là một trợ lý AI thông minh, hiểu biết sâu rộng, thân thiện và giao tiếp tự nhiên như ChatGPT trên website Trường Đại học Công nghiệp Việt – Hung (VIU).

            PHONG CÁCH VÀ NĂNG LỰC TRÒ CHUYỆN:
            1. Bạn có thể trò chuyện cởi mở, thông minh, tinh tế và hữu ích về MỌI chủ đề: học tập, lập trình, công nghệ, cuộc sống, tư vấn hướng nghiệp, giải bài tập, viết lách, văn hóa, khoa học, dịch thuật, hoặc tâm sự, giải trí nhẹ nhàng.
            2. Trả lời tự nhiên, ấm áp, lịch sự và sinh động bằng tiếng Việt (hoặc ngôn ngữ người dùng dùng nếu họ nói tiếng khác).
            3. Trình bày câu trả lời rõ ràng, mạch lạc, có chiều sâu. Sử dụng Markdown tự nhiên (in đậm **từ khóa**, danh sách gạch đầu dòng, bảng biểu, code block, trích dẫn...) để câu trả lời sinh động, dễ đọc.
            4. Tuyệt đối KHÔNG trả lời cộc lốc hoặc rập khuôn máy móc. Hãy trả lời chu đáo, hữu ích như một người bạn kiêm cố vấn thông minh.

            KHI NGƯỜI DÙNG HỎI VỀ TRƯỜNG ĐẠI HỌC CÔNG NGHIỆP VIỆT – HUNG (VIU):
            1. Nếu câu hỏi liên quan đến trường (ngành học, học phí, điểm chuẩn, cơ sở vật chất, tuyển sinh, liên hệ...), hãy ưu tiên sử dụng thông tin chính xác từ phần [DỮ LIỆU THAM KHẢO VỀ TRƯỜNG] nếu được cung cấp.
            2. Với các số liệu như điểm chuẩn chính thức hoặc học phí, hãy bám sát dữ liệu thực tế được cung cấp. Nếu dữ liệu chưa có, hãy giải thích rõ và hướng dẫn người dùng liên hệ hotline hoặc trang thông tin của trường.
            """;

        public async Task<ChatAnswer> AskAsync(
            string question,
            IReadOnlyList<ChatTurn> history,
            CancellationToken ct)
        {
            question = (question ?? string.Empty).Trim();
            if (question.Length == 0)
            {
                return Unavailable("Bạn vui lòng nhập câu hỏi hoặc nội dung muốn trò chuyện.");
            }

            if (question.Length > 2000)
                question = question[..2000];

            var knowledge = await _knowledge.BuildAsync(question, ct);
            var hasFacts = knowledge.HasTopicFacts;

            if (!IsConfigured)
            {
                return hasFacts
                    ? new ChatAnswer
                    {
                        Reply = "<p>Trợ lý AI chưa được kích hoạt nên hệ thống hiện chỉ tra cứu được dữ liệu trực tiếp.</p>",
                        Source = AnswerSource.Unavailable,
                        SourceNote = "Chưa cấu hình dịch vụ AI",
                        Links = knowledge.Links,
                        Suggestions = knowledge.FollowUps
                    }
                    : Unavailable(
                        "Trợ lý AI chưa được cấu hình. Bạn vui lòng liên hệ hotline tuyển sinh để được hỗ trợ trực tiếp.");
            }

            var turns = new List<ChatTurn> { new("system", SystemPrompt) };

            // Giữ lịch sử hội thoại gần nhất để duy trì ngữ cảnh trò chuyện mượt mà như ChatGPT.
            foreach (var turn in history.TakeLast(10))
            {
                if (turn.Role is "user" or "assistant")
                    turns.Add(new ChatTurn(turn.Role, turn.Content[..Math.Min(turn.Content.Length, 1500)]));
            }

            var userPrompt = BuildUserTurn(question, knowledge.Text);
            turns.Add(new ChatTurn("user", userPrompt));

            var reply = await _provider.CompleteAsync(turns, ct);

            if (string.IsNullOrWhiteSpace(reply))
            {
                _log.LogWarning("Assistant returned no content for provider {Provider}", _provider.Name);

                return hasFacts
                    ? new ChatAnswer
                    {
                        Reply = "<p>Dịch vụ AI tạm thời gián đoạn. Bạn có thể xem các thông tin tham khảo bên dưới hoặc liên hệ nhà trường.</p>",
                        Source = AnswerSource.Unavailable,
                        SourceNote = "Dịch vụ AI tạm thời không phản hồi",
                        Links = knowledge.Links,
                        Suggestions = knowledge.FollowUps
                    }
                    : Unavailable(
                        "Dịch vụ AI tạm thời không phản hồi. Bạn vui lòng thử lại sau giây lát.");
            }

            var formattedHtml = RenderMarkdownToSafeHtml(reply);

            return new ChatAnswer
            {
                Reply = formattedHtml,
                Source = hasFacts ? AnswerSource.Grounded : AnswerSource.General,
                SourceNote = hasFacts ? "Thông tin từ dữ liệu nhà trường" : null,
                Links = hasFacts ? knowledge.Links : new List<ChatLink>(),
                Suggestions = hasFacts ? knowledge.FollowUps : new List<string>()
            };
        }

        private static string BuildUserTurn(string question, string knowledge)
        {
            if (string.IsNullOrWhiteSpace(knowledge))
                return question;

            return $"{question}\n\n[DỮ LIỆU THAM KHẢO VỀ TRƯỜNG ĐH CÔNG NGHIỆP VIỆT - HUNG]:\n{knowledge}";
        }

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

        private static readonly MarkdownPipeline MarkdownPipe = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        private static string RenderMarkdownToSafeHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var html = Markdown.ToHtml(markdown, MarkdownPipe);
            var sanitizer = new HtmlSanitizerAdapter();
            return sanitizer.Sanitize(html);
        }
    }

    /// <summary>Thin adapter over HtmlSanitizer allowing rich formatted Markdown elements safely.</summary>
    internal sealed class HtmlSanitizerAdapter
    {
        private readonly Ganss.Xss.HtmlSanitizer _sanitizer = Create();

        private static Ganss.Xss.HtmlSanitizer Create()
        {
            var s = new Ganss.Xss.HtmlSanitizer();
            s.AllowedTags.Add("p");
            s.AllowedTags.Add("h1");
            s.AllowedTags.Add("h2");
            s.AllowedTags.Add("h3");
            s.AllowedTags.Add("h4");
            s.AllowedTags.Add("h5");
            s.AllowedTags.Add("h6");
            s.AllowedTags.Add("a");
            s.AllowedTags.Add("ul");
            s.AllowedTags.Add("ol");
            s.AllowedTags.Add("li");
            s.AllowedTags.Add("strong");
            s.AllowedTags.Add("b");
            s.AllowedTags.Add("em");
            s.AllowedTags.Add("i");
            s.AllowedTags.Add("br");
            s.AllowedTags.Add("hr");
            s.AllowedTags.Add("code");
            s.AllowedTags.Add("pre");
            s.AllowedTags.Add("blockquote");
            s.AllowedTags.Add("table");
            s.AllowedTags.Add("thead");
            s.AllowedTags.Add("tbody");
            s.AllowedTags.Add("tr");
            s.AllowedTags.Add("th");
            s.AllowedTags.Add("td");

            s.AllowedAttributes.Add("href");
            s.AllowedAttributes.Add("target");
            s.AllowedAttributes.Add("rel");
            s.AllowedAttributes.Add("class");

            s.AllowedSchemes.Add("http");
            s.AllowedSchemes.Add("https");
            s.AllowedSchemes.Add("mailto");
            s.AllowedSchemes.Add("tel");
            return s;
        }

        public string Sanitize(string html) => _sanitizer.Sanitize(html);
    }
}