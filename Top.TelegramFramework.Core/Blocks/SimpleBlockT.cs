
namespace Top.TelegramFramework.Core.Blocks
{
    public delegate bool ValidatorDelegate(string userMessage, out string errorMessage);
    public delegate TResponse HandlerDelegate<TResponse>(string userMessage);

    public class SimpleBlock<TResponse>
    {
        public ValidatorDelegate Validator { get; set; }
        public HandlerDelegate<TResponse> Handler { get; set; }

        public SimpleBlock(ValidatorDelegate validator, HandlerDelegate<TResponse> handler)
        {
            Validator = validator;
            Handler = handler;
        }

        /// <summary>
        /// Обработка сообщения с валидацией
        /// </summary>
        public async Task<(bool IsValid, TResponse Response, string ErrorMessage)> ProcessAsync(string userMessage)
        {
            // Простая синхронная валидация, можно обернуть в Task.Run при необходимости
            if (!Validator(userMessage, out string error))
            {
                return (false, default, error);
            }

            var response = Handler(userMessage);
            return (true, response, null);
        }
    }
}
