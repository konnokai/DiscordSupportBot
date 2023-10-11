using System.ComponentModel.DataAnnotations;

namespace DiscordSupportBot.SQLite.Table
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
