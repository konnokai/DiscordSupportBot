using DiscordSupportBot.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordSupportBot.Tests;

public class SupportContextMigrationTests
{
    [Fact]
    public void Migrate_CreatesSchemaAndRecordsButtonRoleMigration()
    {
        ResetDatabase();

        using var db = new SupportContext();
        db.Database.Migrate();

        Assert.Empty(db.ButtonRole);
        Assert.Contains("20261112112245_AddButtonRole", db.Database.GetAppliedMigrations());
    }

    [Fact]
    public void Migrate_FromPreviousMigration_AppliesOnlyButtonRole()
    {
        ResetDatabase();

        using var db = new SupportContext();
        db.GetService<IMigrator>().Migrate("20261106044854_AddLotteryWinnerColumns");

        Assert.DoesNotContain("20261112112245_AddButtonRole", db.Database.GetAppliedMigrations());

        db.Database.Migrate();

        Assert.Contains("20261112112245_AddButtonRole", db.Database.GetAppliedMigrations());
        Assert.Empty(db.ButtonRole);
    }

    private static void ResetDatabase()
    {
        string dbPath = Program.GetDataFilePath("DataBase.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            File.Delete(dbPath + suffix);
    }
}
