
using System.ComponentModel.DataAnnotations;

namespace Top.TelegramFramework.Core.Data
{
    public class UserStateEntity
    {
        [Key]
        public long ChatId { get; set; }

        [Key]
        public string ScenarioId { get; set; }

        public string StateJson { get; set; }
    }
}
