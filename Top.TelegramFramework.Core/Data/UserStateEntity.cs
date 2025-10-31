// File: Top.TelegramFramework.Core/Data/UserStateEntity.cs

namespace Top.TelegramFramework.Core.Data
{
    public class UserStateEntity
    {
        public long ChatId { get; set; }
        public string ScenarioId { get; set; } = string.Empty;
        public string CurrentBlockId { get; set; } = string.Empty;
        public string? StateJson { get; set; }
        public string? ContextJson { get; set; }
    }
}
