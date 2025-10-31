// File: Top.TelegramFramework.Core/Data/UserState.cs
using System.Collections.Generic;

namespace Top.TelegramFramework.Core.Data
{
    // DTO, содержащий текущий блок, JSON состояния блока и словарь контекста
    public class UserState
    {
        public string CurrentBlockId { get; set; } = string.Empty;
        public string? StateJson { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }
}
