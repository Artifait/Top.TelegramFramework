// File: Top.TelegramFramework.Core/ScenarioSelector.cs

namespace Top.TelegramFramework.Core
{
    // Selector выбирает сценарий по правилам; имеет обязательный default scenario
    public class ScenarioSelector
    {
        private readonly List<(Scenario scenario, Func<long, bool> condition)> _rules = new();
        private Scenario? _defaultScenario;

        public void Register(Scenario scenario, Func<long, bool> condition)
        {
            _rules.Add((scenario, condition));
        }

        public void SetDefault(Scenario scenario)
        {
            _defaultScenario = scenario;
        }

        public Scenario GetScenarioForUser(long chatId)
        {
            foreach (var (scenario, condition) in _rules)
            {
                if (condition(chatId)) return scenario;
            }
            if (_defaultScenario == null) throw new InvalidOperationException("Default scenario not set");
            return _defaultScenario;
        }
    }
}
