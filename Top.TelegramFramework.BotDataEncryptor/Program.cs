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

            Console.WriteLine($"Зашифрованый токен: {AESCryptography.Encrypt(token!, mainPassword!)}");
            Console.WriteLine($"Зашифрованый пароль: {PasswordManager.HashPassword(mainPassword!)}");
        }
    }
}
