
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Top.TelegramFramework.Core;

namespace Top.TelegramFramework
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTelegramFramework(this IServiceCollection services, Action<TelegramFrameworkOptions> configure)
        {
            var options = new TelegramFrameworkOptions();
            configure(options);

            if (!string.IsNullOrEmpty(options.Token))
            {
                services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(options.Token));
            }

            var selector = new ScenarioSelector();
            var registeredTypes = new HashSet<Type>();
            bool defaultSet = false;

            foreach (var reg in options.Registrations)
            {
                Scenario scenario;

                if (reg.ScenarioInstance != null)
                {
                    // Сценарий передан напрямую
                    scenario = reg.ScenarioInstance;
                }
                else
                {
                    // Создаём новый сценарий и собираем блоки
                    if (reg.InitialBlockType == null)
                        throw new InvalidOperationException("InitialBlockType must be provided for registration");

                    scenario = new Scenario(reg.ScenarioId);

                    if (reg.OnlyNamespace)
                        scenario.RegisterBlocksFromNamespace(reg.InitialBlockType);
                    else
                        scenario.RegisterBlocksFromAssembly(reg.InitialBlockType);

                    scenario.RegisterInitialBlockType(reg.InitialBlockType);
                }

                // Добавляем в selector с predicate (если есть)
                if (reg.Predicate != null)
                    selector.Register(scenario, reg.Predicate);

                if (reg.IsDefault && !defaultSet)
                {
                    selector.SetDefault(scenario);
                    defaultSet = true;
                }

                // Собираем типы блоков для регистрации в DI
                foreach (var t in scenario.GetRegisteredBlockTypes())
                {
                    if (registeredTypes.Add(t))
                    {
                        services.AddTransient(t);
                    }
                }
            }

            // Если default не выставлен — выберем первый сценарий без predicate или первый вообще
            if (!defaultSet)
            {
                var firstNoPredicate = options.Registrations.FirstOrDefault(r => r.Predicate == null);
                if (firstNoPredicate != null)
                {
                    Scenario s;
                    if (firstNoPredicate.ScenarioInstance != null) s = firstNoPredicate.ScenarioInstance;
                    else
                    {
                        s = new Scenario(firstNoPredicate.ScenarioId);
                        if (firstNoPredicate.InitialBlockType != null)
                        {
                            if (firstNoPredicate.OnlyNamespace) s.RegisterBlocksFromNamespace(firstNoPredicate.InitialBlockType);
                            else s.RegisterBlocksFromAssembly(firstNoPredicate.InitialBlockType);
                            s.RegisterInitialBlockType(firstNoPredicate.InitialBlockType);
                        }
                    }
                    selector.SetDefault(s);
                }
            }

            services.AddSingleton(selector);
            services.AddHostedService<BotEngine>();

            return services;
        }
    }
}
