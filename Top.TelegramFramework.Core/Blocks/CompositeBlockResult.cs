
namespace Top.TelegramFramework.Core.Blocks
{
    public class CompositeBlockResult
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

        public static CompositeBlockResult Error(string message, Dictionary<string, object> data = null)
        {
            return new CompositeBlockResult 
            { 
                resultState = ResultState.IsError,
                ErrorMessage = message, Data = data ?? []
            };
        }

        public static CompositeBlockResult End(string? nextBlockId = null, Dictionary<string, object>? data = null)
        {
            return new CompositeBlockResult 
            { 
                NextBlockId = nextBlockId,
                resultState = ResultState.IsEnd,
                Data = data ?? []
            };
        }

        public static CompositeBlockResult Continue(Dictionary<string, object>? data = null)
        {
            return new CompositeBlockResult 
            { 
                resultState = ResultState.IsContinue, 
                Data = data ?? []
            };
        }
    }
}
