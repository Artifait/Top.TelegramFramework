
namespace Top.TelegramFramework.Core.Blocks
{
    public enum HandlerBlockResultState
    {
        IsContinue,
        IsError,
        IsEnd
    }

    public class HandlerBlockResult
    {
        public HandlerBlockResultState ResultState { get; }
        public string? ErrorMessage { get; }
        public Dictionary<string, object>? Data { get; }
        public string? NextBlockId { get; }
        public bool ReEnter { get; } // если true — после Continue вызвать EnterAsync того же блока

        public HandlerBlockResult(HandlerBlockResultState state, string? error = null, Dictionary<string, object>? data = null, string? nextBlockId = null, bool reEnter = false)
        {
            ResultState = state;
            ErrorMessage = error;
            Data = data;
            NextBlockId = nextBlockId;
            ReEnter = reEnter;
        }

        public static HandlerBlockResult Error(string message, Dictionary<string, object>? data = null) =>
            new HandlerBlockResult(HandlerBlockResultState.IsError, message, data);

        public static HandlerBlockResult Continue(Dictionary<string, object>? data = null, bool reEnter = false) =>
            new HandlerBlockResult(HandlerBlockResultState.IsContinue, null, data, null, reEnter);

        public static HandlerBlockResult End(string? nextBlockId = null, Dictionary<string, object>? data = null) =>
            new HandlerBlockResult(HandlerBlockResultState.IsEnd, null, data, nextBlockId);
    }
}
