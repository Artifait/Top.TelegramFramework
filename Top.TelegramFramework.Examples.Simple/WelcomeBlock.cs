
using Telegram.Bot.Types;
using Top.TelegramFramework.Core.Blocks;
using Top.TelegramFramework.Core.Utilities;

namespace Top.TelegramFramework.Examples.Simple
{
    [Block("welcome")]
    public class WelcomeBlock : Block<WelcomeState>
    {
        public override string BlockId => "welcome";

        public override async Task EnterAsync(BlockContext ctx, CancellationToken ct)
        {
            await ctx.ReplyAsync("Добро пожаловать! Пожалуйста, введите своё имя:");
        }

        public override async Task<HandlerBlockResult> HandleAsync(Message message, BlockContext ctx, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
                return Return.Error("Имя не может быть пустым");

            State.Count++;
            await ctx.ReplyAsync($"Твоё имя: {message.Text}.\nТы бежишь в этом колесе {State.Count} круг!");

            // Сохраняем состояние и повторно вызываем EnterAsync (без End)
            return Return.Continue(reEnter: true);
        }
    }
}
