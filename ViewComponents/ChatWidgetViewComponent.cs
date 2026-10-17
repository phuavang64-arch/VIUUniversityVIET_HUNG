using Microsoft.AspNetCore.Mvc;

namespace VIUUniversity.ViewComponents
{
    /// <summary>
    /// Renders the admission assistant shell.
    ///
    /// Marked as a view component (not a static partial) so the "AI is configured"
    /// state can be decided on the server. The API key is never involved here -
    /// only a boolean reaches the browser.
    /// </summary>
    public class ChatWidgetViewComponent : ViewComponent
    {
        private readonly Services.Chat.IChatService _chat;

        public ChatWidgetViewComponent(Services.Chat.IChatService chat)
        {
            _chat = chat;
        }

        public IViewComponentResult Invoke() => View(new ChatWidgetModel
        {
            IsConfigured = _chat.IsConfigured,
            Endpoint = _chat.Endpoint
        });
    }

    public class ChatWidgetModel
    {
        public bool IsConfigured { get; init; }
        public string Endpoint { get; init; } = "/api/chat";
    }
}