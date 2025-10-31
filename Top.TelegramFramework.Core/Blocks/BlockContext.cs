// File: Top.TelegramFramework.Core/Blocks/BlockContext.cs

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;

namespace Top.TelegramFramework.Core.Blocks
{
    // Контекст выполнения блока: содержит клиент, chat id, вспомогательный словарь ContextBag и методы отправки
    public class BlockContext
    {
        public ITelegramBotClient? Client { get; set; }
        public long ChatId { get; set; }
        public int? CallbackMessageId { get; set; }
        public string? ScenarioId { get; set; }
        public IDictionary<string, object> ContextBag { get; set; } = new Dictionary<string, object>();
        public ILogger? Logger { get; set; }

        public Task<Message> ReplyAsync(string text, InlineKeyboardMarkup? markup = null, CancellationToken ct = default)
        {
            if (Client == null) throw new InvalidOperationException("Client not set on BlockContext");
            if (CallbackMessageId.HasValue)
            {
                return Client.EditMessageText(ChatId, CallbackMessageId.Value, text, replyMarkup: markup, cancellationToken: ct);
            }
            return Client.SendMessage(ChatId, text, replyMarkup: markup, cancellationToken: ct);
        }

        public Task<Message> SendAsync(string text, InlineKeyboardMarkup? markup = null, CancellationToken ct = default)
        {
            if (Client == null) throw new InvalidOperationException("Client not set on BlockContext");
            return Client.SendMessage(ChatId, text, replyMarkup: markup, cancellationToken: ct);
        }

        public Task EditAsync(int messageId, string text, InlineKeyboardMarkup? markup = null, CancellationToken ct = default)
        {
            if (Client == null) throw new InvalidOperationException("Client not set on BlockContext");
            return Client.EditMessageText(ChatId, messageId, text, replyMarkup: markup, cancellationToken: ct);
        }

        public Task AnswerCallbackAsync(string callbackQueryId, string? text = null, CancellationToken ct = default)
        {
            if (Client == null) throw new InvalidOperationException("Client not set on BlockContext");
            return Client.AnswerCallbackQuery(callbackQueryId, text, cancellationToken: ct);
        }
    }
}
