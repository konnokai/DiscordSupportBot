using Microsoft.EntityFrameworkCore;

namespace DiscordSupportBot.SQLite
{
    class SupportContext : DbContext
    {
        public DbSet<TrustedGuild> TrustedGuild { get; set; }
        public DbSet<GuildConfig> GuildConfig { get; set; }
        public DbSet<NCChannelCOD> NCChannelCOD { get; set; }
        public DbSet<Lottery> Lottery { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={Program.GetDataFilePath("DataBase.db")}");
    }
}
