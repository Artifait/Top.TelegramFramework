// File: Top.TelegramFramework.Core/Data/IStateStore.cs

namespace Top.TelegramFramework.Core.Data
{
    // Абстракция хранилища состояния пользователя
    public interface IStateStore
    {
        Task<UserState?> GetAsync(long chatId, string scenarioId, CancellationToken ct = default);
        Task SaveAsync(long chatId, string scenarioId, string blockId, string? stateJson, IDictionary<string, object>? context, CancellationToken ct = default);
        Task DeleteAsync(long chatId, string scenarioId, CancellationToken ct = default);
    }
}
