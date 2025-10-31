// File: Top.TelegramFramework.Core/Utilities/Return.cs
using Top.TelegramFramework.Core.Blocks;

namespace Top.TelegramFramework.Core.Utilities
{
    // Фабрики результатов блоков
    public static class Return
    {
        public static HandlerBlockResult Continue(Dictionary<string, object>? data = null, bool reEnter = false) =>
            HandlerBlockResult.Continue(data, reEnter);

        public static HandlerBlockResult End(string? next = null, Dictionary<string, object>? data = null) =>
            HandlerBlockResult.End(next, data);

        public static HandlerBlockResult Error(string message, Dictionary<string, object>? data = null) =>
            HandlerBlockResult.Error(message, data);
    }
}
