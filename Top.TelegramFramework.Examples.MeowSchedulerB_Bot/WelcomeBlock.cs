
using Telegram.Bot.Types;
using Telegram.Bot;
using Top.TelegramFramework.Core.Blocks;
using Telegram.Bot.Types.ReplyMarkups;

namespace Top.TelegramFramework.Examples.Simple
{
    public class WelcomeBlock : CompositeBlock
    {
        public static readonly InlineKeyboardMarkup _welcomeMarkup = new InlineKeyboardMarkup()
            .AddButton("КОТЫ", "Cats")
            .AddButton("Добавить кота", "AddCat");

        public override string BlockId => "welcome";

        public override async Task EnterAsync(BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct)
        {
            Int64? pastMessageId = context.State.TryGetValue("messageId", out object? messageId) ? (Int64)messageId : null;
            if (pastMessageId == null) 
                await bot.SendMessage(context.ChatId, "Добро пожаловать! Что вы хотите сделать?", cancellationToken: ct, replyMarkup: _welcomeMarkup);
        }

        public override async Task<CompositeBlockResult> HandleAsync(Message message, BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                return CompositeBlockResult.Error("Имя не может быть пустым");
            }
            Int64 count = context.State.TryGetValue("Count", out object? obj) ? (Int64)obj : 1;


            await bot.SendMessage(context.ChatId, $"Твоё имя: {message.Text}.\nТы бежишь в этом колесе {count} круг!", cancellationToken: ct);

            // Передаём количество в контекст для следующего блока.
            return CompositeBlockResult.End(
                nextBlockId: BlockId,
                data: new Dictionary<string, object>
                {
                    { "Count", ++count }
                }
            );
        }

        public override CompositeBlock Clone()
            => new WelcomeBlock();

        public override void OnEnd() { }

        public override async Task<CompositeBlockResult> HandleCallbackAsync(CallbackQuery callback, BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct)
        {
            var chatId = callback.Message!.Chat.Id;
            var messageId = callback.Message!.MessageId;

            switch (callback.Data)
            {
                case "Cats":
                    await bot.SendMessage(chatId, "Нету блока для просмотра добавленных котов", cancellationToken: ct);
                    break;
                case "AddCat":
                    await bot.SendMessage(chatId, "Нету блока для добавления котов", cancellationToken: ct);
                    break;
            }

            return CompositeBlockResult.End(nextBlockId: BlockId, data: new Dictionary<string, object> { { "messageId", messageId } });

        }
    }
}
