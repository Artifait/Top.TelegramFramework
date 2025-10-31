// File: Top.TelegramFramework.Core/Data/UserStateContext.cs
using Microsoft.EntityFrameworkCore;

namespace Top.TelegramFramework.Core.Data
{
    public class UserStateContext : DbContext
    {
        public DbSet<UserStateEntity> UserStates { get; set; } = null!;

        public UserStateContext(DbContextOptions<UserStateContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserStateEntity>().HasKey(u => new { u.ChatId, u.ScenarioId });
            base.OnModelCreating(modelBuilder);
        }
    }
}
