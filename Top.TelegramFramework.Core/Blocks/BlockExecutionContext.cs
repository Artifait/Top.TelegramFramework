
namespace Top.TelegramFramework.Core.Blocks
{
    public class BlockExecutionContext
    {
        /// <summary>
        /// Идентификатор чата (или другой идентификатор пользователя)
        /// </summary>
        public long ChatId { get; set; }

        /// <summary>
        /// Контекст (данные, переданные от предыдущего блока)
        /// </summary>
        public Dictionary<string, object> State { get; set; } = new Dictionary<string, object>();
    }
}
