// File: Top.TelegramFramework.Core/Blocks/HandlerBlock.cs

using Telegram.Bot.Types;

namespace Top.TelegramFramework.Core.Blocks
{
    // Базовый блок: можно переопределять ApplyState/CaptureState для хранения состояния
    public abstract class HandlerBlock
    {
        public abstract string BlockId { get; }

        public virtual Task EnterAsync(BlockContext context, CancellationToken ct) => Task.CompletedTask;

        public virtual Task<HandlerBlockResult> HandleAsync(Message message, BlockContext context, CancellationToken ct) =>
            Task.FromResult(HandlerBlockResult.Continue());

        public virtual Task<HandlerBlockResult> HandleCallbackAsync(Telegram.Bot.Types.CallbackQuery callbackQuery, BlockContext context, CancellationToken ct) =>
            Task.FromResult(HandlerBlockResult.Continue());

        public virtual void OnEnd() { }

        // state serialization hooks (json) — null means no state
        public virtual void ApplyState(string? stateJson) { }

        public virtual string? CaptureState() => null;
    }
}
