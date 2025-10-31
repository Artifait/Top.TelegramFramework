
using Top.TelegramFramework.Core;
using Top.TelegramFramework.Core.Blocks;

namespace Top.TelegramFramework
{
    public class TelegramFrameworkOptions
    {
        internal List<ScenarioRegistration> Registrations { get; } = new();

        public string? Token { get; set; }

        public void AddScenario<TInit>(string scenarioId, Func<long, bool>? predicate = null, bool isDefault = false, bool onlyNamespace = false)
            where TInit : HandlerBlock
        {
            Registrations.Add(new ScenarioRegistration
            {
                ScenarioId = scenarioId,
                InitialBlockType = typeof(TInit),
                Predicate = predicate,
                IsDefault = isDefault,
                OnlyNamespace = onlyNamespace
            });
        }

        public void AddScenarioInstance(Scenario scenario, Func<long, bool>? predicate = null, bool isDefault = false)
        {
            Registrations.Add(new ScenarioRegistration
            {
                ScenarioInstance = scenario,
                ScenarioId = scenario.ScenarioId,
                Predicate = predicate,
                IsDefault = isDefault
            });
        }
    }

    internal class ScenarioRegistration
    {
        public string ScenarioId { get; set; } = string.Empty;
        public Type? InitialBlockType { get; set; }
        public bool OnlyNamespace { get; set; } = false;
        public Func<long, bool>? Predicate { get; set; }
        public bool IsDefault { get; set; } = false;
        public Scenario? ScenarioInstance { get; set; }
    }
}
