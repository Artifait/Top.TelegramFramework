
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Top.TelegramFramework.Core.Data;

namespace Top.TelegramFramework.Examples.Simple
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var token = AppData.TelegramToken;
            if (string.IsNullOrWhiteSpace(token) || token.Contains("<YOUR"))
            {
                Console.WriteLine("Please set AppData.TelegramToken before running.");
                return;
            }

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging((ctx, lb) => lb.AddConsole())
                .ConfigureServices((ctx, services) =>
                {
                    services.AddDbContext<UserStateContext>(opts => opts.UseSqlite("Data Source=userstates.db"));
                    services.AddScoped<IStateStore, EfStateStore>();

                    // Автоматическая регистрация фреймворка
                    services.AddTelegramFramework(options =>
                    {
                        options.Token = token;

                        // 1) Default сценарий: регистрируем только блоки из namespace WelcomeBlock (и под-namespace'ов)
                        options.AddScenario<WelcomeBlock>("default", predicate: null, isDefault: true, onlyNamespace: true);

                        //// 2) Custom scenario, построенный вручную (может содержать блоки из разных namespace'ов)
                        //var special = new Scenario("special_for_1302680066");
                        //// вручную регистрируем необходимые блоки (например блоки из разных namespace'ов)
                        //// special.RegisterBlock("welcome", typeof(WelcomeBlock));
                        //// special.RegisterBlock("other", typeof(Some.OtherNamespace.OtherBlock));
                        //// укажем стартовый блок явно:
                        //special.RegisterInitialBlockType(typeof(WelcomeBlock));

                        //options.AddScenarioInstance(special, predicate: chatId => chatId == 1302680066, isDefault: false);
                    });
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<UserStateContext>();
                ctx.Database.EnsureCreated();
            }

            await host.RunAsync();
        }
    }
}
