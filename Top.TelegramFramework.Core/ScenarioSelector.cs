
namespace Top.TelegramFramework.Core
{
    public class ScenarioSelector
    {
        private readonly List<(Scenario scenario, Func<long, bool> condition)> _rules = new List<(Scenario, Func<long, bool>)>();
        private Scenario _defaultScenario;

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
            var scenario = _rules.FirstOrDefault(rule => rule.condition(chatId)).scenario;
            return scenario ?? _defaultScenario;
        }
    }
}
