
using Top.TelegramFramework.Core;
using Top.TelegramFramework.Core.Utilities;

namespace Top.TelegramFramework.Examples.Simple
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            string mainPassword = HiddenInput.ReadHiddenInput();
            if (!PasswordManager.VerifyHashedPassword(AppData.MainPasswordHash, mainPassword))
            {
                Console.WriteLine("Не верный пароль...");
                return;
            }

            var token = AESCryptography.Decrypt(AppData.TokenHash, mainPassword);

            // Создаём сценарий с уникальным идентификатором
            var defaultScenario = new Scenario("default");

            // Регистрируем составной блок ClickerBlock в сценарии
            defaultScenario.RegisterBlock(new WelcomeBlock());

            // Регистрируем сценарий в селекторе сценариев. Здесь можно задать условие, при котором выбирается данный сценарий.
            // Для простоты можно сделать его сценарий по умолчанию.
            var scenarioSelector = new ScenarioSelector();
            scenarioSelector.SetDefault(defaultScenario);

            // Передаём scenarioSelector в конструктор BotEngine, который теперь содержит всю логику инициализации бота, базы и т.д.
            var botEngine = new BotEngine(scenarioSelector);

            await botEngine.StartBot(token);
        }
    } 
}
