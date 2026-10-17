using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace VIUUniversity.Services.Chat
{
    /// <summary>
    /// OpenAI provider dùng Responses API qua OpenAI .NET SDK (v2.x).
    ///
    /// SDK đánh dấu Responses API là "evaluation only" (chẩn đoán OPENAI001); chẩn đoán
    /// này được tắt trong .csproj kèm chú thích. Xem lại khi nâng phiên bản SDK.
    ///
    /// API key đọc theo thứ tự: biến môi trường OPENAI_API_KEY, sau đó mới tới
    /// cấu hình Chat:ApiKey. Key KHÔNG BAO GIỜ được ghi log, in ra terminal,
    /// trả về trình duyệt hay nhúng vào mã frontend.
    /// </summary>
    public sealed class OpenAiResponsesProvider : IChatProvider
    {
        private readonly ChatOptions _options;
        private readonly ILogger<OpenAiResponsesProvider> _log;
        private readonly ResponsesClient _client;

        public OpenAiResponsesProvider(
            IOptions<ChatOptions> options,
            ILogger<OpenAiResponsesProvider> log)
        {
            // Phải nhận IOptions<> chứ không nhận ChatOptions trực tiếp: DI chỉ đăng ký
            // ChatOptions qua Configure<>, tức là IOptions<ChatOptions>.
            _options = options.Value;
            _log = log;

            var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(key))
                key = _options.ApiKey;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    "Chưa cấu hình OPENAI_API_KEY cho trợ lý AI.");

            _client = new ResponsesClient(key);
        }

        public string Name => "openai-responses";

        public async Task<string?> CompleteAsync(
            IReadOnlyList<ChatTurn> turns,
            CancellationToken ct)
        {
            if (turns.Count == 0)
                return null;

            var options = new CreateResponseOptions
            {
                Model = _options.Model,
                MaxOutputTokenCount = _options.MaxTokens,
                Temperature = 0.2f
            };

            // System/developer instructions tách khỏi lịch sử hội thoại.
            var system = turns.FirstOrDefault(t => t.Role is "system" or "developer");
            if (system != null)
                options.Instructions = system.Content;

            // Lịch sử nhiều lượt để trợ lý hiểu câu hỏi tiếp nối.
            foreach (var turn in turns)
            {
                switch (turn.Role)
                {
                    case "user":
                        options.InputItems.Add(ResponseItem.CreateUserMessageItem(turn.Content));
                        break;

                    case "assistant":
                        options.InputItems.Add(
                            ResponseItem.CreateAssistantMessageItem(
                                turn.Content, Array.Empty<ResponseMessageAnnotation>()));
                        break;
                }
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            try
            {
                var result = await _client.CreateResponseAsync(options, timeout.Token);

                // Responses API có thể trả HTTP 200 kèm lỗi nội bộ — phải kiểm tra.
                if (result.Value?.Error is { } apiError)
                {
                    _log.LogWarning("OpenAI tra ve loi trong phan hoi: {Code}", apiError.Code);
                    return null;
                }

                var text = result.Value?.GetOutputText();

                if (string.IsNullOrWhiteSpace(text))
                {
                    _log.LogWarning("OpenAI tra ve noi dung rong cho model {Model}", _options.Model);
                    return null;
                }

                return text;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Người dùng đã ngắt — không phải lỗi hệ thống.
                throw;
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("OpenAI het thoi gian sau {Seconds}s", _options.TimeoutSeconds);
                return null;
            }
            catch (ClientResultException ex)
            {
                // Chỉ log mã trạng thái, KHÔNG log nội dung request (có thể chứa dữ liệu người dùng).
                if (ex.Status is 401 or 403)
                {
                    _log.LogError(
                        "OpenAI tu choi xac thuc (HTTP {Status}). Kiem tra OPENAI_API_KEY " +
                        " tren may chu va con han muc su dung.", (int)ex.Status);
                }
                else if (ex.Status is 429)
                {
                    _log.LogWarning("OpenAI vuot han muc su dung (HTTP 429).");
                }
                else
                {
                    _log.LogWarning("OpenAI tra ve loi HTTP {Status}", (int)ex.Status);
                }

                return null;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Loi khi goi OpenAI Responses API");
                return null;
            }
        }
    }
}