using System.ComponentModel.DataAnnotations;

namespace DiscordSupportBot.DataBase.Table
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
