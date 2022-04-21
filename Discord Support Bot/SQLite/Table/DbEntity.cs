using System.ComponentModel.DataAnnotations;

namespace Discord_Support_Bot.SQLite.Table
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
