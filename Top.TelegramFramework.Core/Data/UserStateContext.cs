
using Microsoft.EntityFrameworkCore;

namespace Top.TelegramFramework.Core.Data
{
    public class UserStateContext : DbContext
    {
        public DbSet<UserStateEntity> UserStates { get; set; }

        public UserStateContext(DbContextOptions<UserStateContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Указываем составной ключ: ChatId + ScenarioId
            modelBuilder.Entity<UserStateEntity>()
                .HasKey(us => new { us.ChatId, us.ScenarioId });
        }
    }
}
