// File: Top.TelegramFramework.Core/Data/EfStateStore.cs
using System.Text.Json;

namespace Top.TelegramFramework.Core.Data
{
    // EF Core реализация IStateStore (SQLite/Postgres/SQLServer через DbContextOptions)
    public class EfStateStore : IStateStore
    {
        private readonly UserStateContext _context;

        public EfStateStore(UserStateContext context)
        {
            _context = context;
        }

        public async Task<UserState?> GetAsync(long chatId, string scenarioId, CancellationToken ct = default)
        {
            var entity = await _context.UserStates.FindAsync(new object[] { chatId, scenarioId }, ct);
            if (entity == null) return null;

            var ctx = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(entity.ContextJson))
            {
                try
                {
                    ctx = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.ContextJson) ?? new Dictionary<string, object>();
                }
                catch
                {
                    ctx = new Dictionary<string, object>();
                }
            }

            return new UserState
            {
                CurrentBlockId = entity.CurrentBlockId,
                StateJson = entity.StateJson,
                Context = ctx
            };
        }

        public async Task SaveAsync(long chatId, string scenarioId, string blockId, string? stateJson, IDictionary<string, object>? context, CancellationToken ct = default)
        {
            var entity = await _context.UserStates.FindAsync(new object[] { chatId, scenarioId }, ct);
            var ctxJson = context == null ? null : JsonSerializer.Serialize(context);
            if (entity == null)
            {
                entity = new UserStateEntity
                {
                    ChatId = chatId,
                    ScenarioId = scenarioId,
                    CurrentBlockId = blockId,
                    StateJson = stateJson,
                    ContextJson = ctxJson
                };
                _context.UserStates.Add(entity);
            }
            else
            {
                entity.CurrentBlockId = blockId;
                entity.StateJson = stateJson;
                entity.ContextJson = ctxJson;
                _context.UserStates.Update(entity);
            }
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(long chatId, string scenarioId, CancellationToken ct = default)
        {
            var entity = await _context.UserStates.FindAsync(new object[] { chatId, scenarioId }, ct);
            if (entity != null)
            {
                _context.UserStates.Remove(entity);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
