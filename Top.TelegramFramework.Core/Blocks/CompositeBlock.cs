
using Telegram.Bot.Types;
using Telegram.Bot;

namespace Top.TelegramFramework.Core.Blocks
{
    public abstract class CompositeBlock
    {
        /// <summary>
        /// Идентификатор блока.
        /// </summary>
        public abstract string BlockId { get; }

        /// <summary>
        /// Внутреннее состояние блока.
        /// </summary>
        protected Dictionary<string, object> InternalState { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Инициализация состояния блока.
        /// </summary>
        public virtual void ApplyState(Dictionary<string, object> state)
        {
            InternalState = state ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Захват состояния блока для сохранения.
        /// </summary>
        public virtual Dictionary<string, object> CaptureState() => new Dictionary<string, object>(InternalState);

        /// <summary>
        /// Вызывается при входе в блок (переход с предыдущего шага).
        /// </summary>
        public abstract Task EnterAsync(BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct);

        /// <summary>
        /// Обработка входящего сообщения в блоке.
        /// </summary>
        public abstract Task<CompositeBlockResult> HandleAsync(Message message, BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct);
        public abstract Task<CompositeBlockResult> HandleCallbackAsync(CallbackQuery callback, BlockExecutionContext context, ITelegramBotClient bot, CancellationToken ct);
        /// <summary>
        /// Метод, вызываемый при завершении работы блока.
        /// </summary>
        public abstract void OnEnd();

        /// <summary>
        /// Метод для клонирования блока.
        /// </summary>
        public abstract CompositeBlock Clone();
    }
}
