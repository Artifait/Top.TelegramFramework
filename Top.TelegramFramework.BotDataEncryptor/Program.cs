using Top.TelegramFramework.Core.Utilities;

namespace Top.TelegramFramework.BotDataEncryptor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Введите токен: ");
            var token = Console.ReadLine();
            Console.Write("Введите главный пароль: ");
            var mainPassword = Console.ReadLine();

            string tokenHash = AESCryptography.Encrypt(token!, mainPassword!);
            string mainPasswordHash = PasswordManager.HashPassword(mainPassword!);

            string resultClass = "    public static class AppData\r\n    {\r\n        public static string MainPasswordHash = \"" + mainPasswordHash + "\";\r\n        public static string TokenHash = \"" + tokenHash + "\";\r\n    }";

            Console.WriteLine(resultClass);
        }
    }
}
