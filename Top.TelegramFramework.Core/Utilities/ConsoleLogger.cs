
namespace Top.TelegramFramework.Core.Utilities
{
    public static class ConsoleLogger
    {
        private static SemaphoreSlim _consoleSemaphore = new(1, 1);
        public static ConsoleColor DefaultColor { get; set; } = Console.ForegroundColor;

        public static async Task LogLine(string msg, LoggedMessageType messageType = LoggedMessageType.Information)
            => await ConsoleLog(msg, Console.WriteLine, messageType);

        public static async Task Log(string msg, LoggedMessageType messageType = LoggedMessageType.Information)
            => await ConsoleLog(msg, Console.Write, messageType);

        public static async Task ConsoleLog(string msg, Action<string> consoleLogger, LoggedMessageType messageType = LoggedMessageType.Information)
        {
            await _consoleSemaphore.WaitAsync();
            try
            {

                Console.ForegroundColor = messageType switch
                {
                    LoggedMessageType.Warning => ConsoleColor.DarkYellow,
                    LoggedMessageType.Errore => ConsoleColor.DarkRed,
                    _ => ConsoleColor.White,
                };

                consoleLogger($"[{Enum.GetName(messageType)}]: {msg}");
            }
            finally { Console.ForegroundColor = DefaultColor; _consoleSemaphore.Release(); }
        }
    }
}
