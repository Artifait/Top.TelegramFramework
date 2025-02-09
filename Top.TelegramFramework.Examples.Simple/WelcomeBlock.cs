
using Telegram.Bot.Types;
using Telegram.Bot;
using Top.TelegramFramework.Core.Blocks;

namespace Top.TelegramFramework.Examples.Simple
{
    public class WelcomeBlock : HandlerBlock
    {
        public override string BlockId => "welcome";

        public override async Task EnterAsync(BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct)
        {
            // Приветственное сообщение при входе в блок.
            await bot.SendMessage(context.ChatId, "Добро пожаловать! Пожалуйста, введите своё имя:", cancellationToken: ct);
        }

        public override async Task<HandlerBlockResult> HandleAsync(Message message, BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                return HandlerBlockResult.Error("Имя не может быть пустым");
            }
            Int64 count = context.State.TryGetValue("Count", out object? obj) ? (Int64)obj : 1;
            await bot.SendMessage(context.ChatId, $"Твоё имя: {message.Text}.\nТы бежишь в этом колесе {count} круг!", cancellationToken: ct);

            // Передаём количество в контекст для следующего блока.
            return HandlerBlockResult.End(
                nextBlockId: BlockId,
                data: new Dictionary<string, object>
                {
                    { "Count", ++count }
                }
            );
        }

        public override WelcomeBlock Clone()
            => new WelcomeBlock();

        public override void OnEnd() { }

        public override Task<HandlerBlockResult> HandleCallbackAsync(CallbackQuery callback, BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct)
            => throw new NotImplementedException();
    }
}
