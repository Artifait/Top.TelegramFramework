
namespace Top.TelegramFramework.Core.Blocks
{
    public class HandlerBlockResult
    {
        public enum ResultState
        {
            IsContinue, // Когда в блоке есть еще простые блоки
            IsError,    // Ошибка
            IsEnd       // Блок завершён
        }
        public ResultState resultState;
        public string? ErrorMessage { get; set; }
        /// <summary>
        /// Данные, которые перейдут в следующий блок(NextBlockId), в виде BlockExecutionContext 
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = [];
        public string? NextBlockId { get; set; }

        public static HandlerBlockResult Error(string message, Dictionary<string, object> data = null)
        {
            return new HandlerBlockResult 
            { 
                resultState = ResultState.IsError,
                ErrorMessage = message, Data = data ?? []
            };
        }

        public static HandlerBlockResult End(string? nextBlockId = null, Dictionary<string, object>? data = null)
        {
            return new HandlerBlockResult 
            { 
                NextBlockId = nextBlockId,
                resultState = ResultState.IsEnd,
                Data = data ?? []
            };
        }

        public static HandlerBlockResult Continue(Dictionary<string, object>? data = null)
        {
            return new HandlerBlockResult 
            { 
                resultState = ResultState.IsContinue, 
                Data = data ?? []
            };
        }
    }
}
