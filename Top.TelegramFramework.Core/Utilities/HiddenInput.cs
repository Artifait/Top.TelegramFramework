namespace Top.TelegramFramework.Core.Utilities
{
    public class HiddenInput
    {
        public static string ReadHiddenInput()
        {
            Console.Write("Введите секретный ключ: ");
            string input = string.Empty;

            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input.Remove(input.Length - 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    input += keyInfo.KeyChar;
                    Console.Write("*");
                }
            }

            return input;
        }
    }
}
