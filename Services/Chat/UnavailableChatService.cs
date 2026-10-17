namespace VIUUniversity.Services.Chat
{
    /// <summary>
    /// Registered when no AI provider is configured.
    ///
    /// It is deliberately NOT a fake model: it reports IsConfigured = false and returns
    /// an explicit "not available" answer, so nobody can mistake canned text for a live
    /// language model. The database-backed assistant (KnowledgeService) still runs.
    /// </summary>
    public class UnavailableChatService : IChatService
    {
        private readonly KnowledgeService _knowledge;

        public UnavailableChatService(KnowledgeService knowledge)
        {
            _knowledge = knowledge;
        }

        public bool IsConfigured => false;

        public string Endpoint => "/api/chat";

        public async Task<ChatAnswer> AskAsync(
            string question,
            IReadOnlyList<ChatTurn> history,
            CancellationToken ct)
        {
            question = (question ?? string.Empty).Trim();
            if (question.Length == 0)
                return new ChatAnswer
                {
                    Reply = "<p>Bạn vui lòng nhập câu hỏi.</p>",
                    Source = AnswerSource.Unavailable
                };

            if (question.Length > 500)
                question = question[..500];

            var knowledge = await _knowledge.BuildAsync(question, ct);

            if (!knowledge.HasTopicFacts)
            {
                return new ChatAnswer
                {
                    Reply = "<p>Mình chưa tìm thấy dữ liệu phù hợp trong hệ thống, và trợ lý AI " +
                            "hiện chưa được cấu hình để hỗ trợ thêm. Bạn vui lòng liên hệ " +
                            "hotline tuyển sinh để được giải đáp trực tiếp.</p>",
                    Source = AnswerSource.Unavailable,
                    SourceNote = "Chưa cấu hình dịch vụ AI",
                    Links = knowledge.Links,
                    Suggestions = knowledge.FollowUps
                };
            }

            // Facts are available even without a model, so present them verbatim rather
            // than pretending a model wrote them.
            return new ChatAnswer
            {
                Reply = "<p>Dưới đây là thông tin tra cứu trực tiếp từ dữ liệu của trường. " +
                        "Trợ lý AI hiện chưa được cấu hình nên mình không diễn giải thêm được.</p>",
                Source = AnswerSource.Grounded,
                SourceNote = "Dữ liệu lấy trực tiếp từ hệ thống của trường",
                Links = knowledge.Links,
                Suggestions = knowledge.FollowUps
            };
        }
    }
}