
namespace Top.TelegramFramework.Core.Data
{
    public class UserStateRepository
    {
        private readonly UserStateContext _context;

        public UserStateRepository(UserStateContext context)
        {
            _context = context;
        }

        public async Task<UserState?> GetUserStateAsync(long chatId, string scenarioId)
        {
            var entity = await _context.UserStates.FindAsync(chatId, scenarioId);
            return entity == null ? null : UserState.Deserialize(entity.StateJson);
        }

        public async Task SaveOrUpdateUserStateAsync(long chatId, string scenarioId, UserState state)
        {
            var entity = await _context.UserStates.FindAsync(chatId, scenarioId);
            if (entity == null)
            {
                entity = new UserStateEntity
                {
                    ChatId = chatId,
                    ScenarioId = scenarioId,
                    StateJson = state.Serialize()
                };
                _context.UserStates.Add(entity);
            }
            else
            {
                entity.StateJson = state.Serialize();
                _context.UserStates.Update(entity);
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserStateAsync(long chatId, string scenarioId)
        {
            var entity = await _context.UserStates.FindAsync(chatId, scenarioId);
            if (entity != null)
            {
                _context.UserStates.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
