using DiscordSupportBot.DataBase.Activity;
using Microsoft.Data.Sqlite;

namespace DiscordSupportBot.Tests;

public class ActivityStoreTests : IDisposable
{
    private const ulong GuildId = 463657254105645056;
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ActivityStoreTests_{Guid.NewGuid():N}.db");
    private readonly ActivityStore _store;

    public ActivityStoreTests()
    {
        _store = new ActivityStore(_dbPath, "SupportBot:Activity:Test", "EmoteID");
    }

    public void Dispose()
    {
        // 連線池會卡住檔案，要先清掉才刪得了
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void GetSqliteCounts_WithoutDatabaseFile_ReturnsEmpty()
    {
        Assert.Empty(_store.GetSqliteCounts(GuildId));
    }

    [Fact]
    public void GetSqliteCounts_WithoutGuildTable_ReturnsEmpty()
    {
        _store.WriteToSqlite(1, [(1, 1)]);

        Assert.Empty(_store.GetSqliteCounts(GuildId));
    }

    [Fact]
    public void WriteToSqlite_InsertsNewIdsAndAddsToExistingIds()
    {
        const ulong snowflakeId = 1_234_567_890_123_456_789;

        _store.WriteToSqlite(GuildId, [(snowflakeId, 3), (2, 5)]);
        _store.WriteToSqlite(GuildId, [(snowflakeId, 4), (3, 1)]);

        var counts = _store.GetSqliteCounts(GuildId);

        Assert.Equal(3, counts.Count);
        Assert.Equal(7, counts[snowflakeId]);
        Assert.Equal(5, counts[2]);
        Assert.Equal(1, counts[3]);
    }

    [Fact]
    public void WriteToSqlite_NegativeDelta_NeverGoesBelowZero()
    {
        _store.WriteToSqlite(GuildId, [(1, 2)]);
        _store.WriteToSqlite(GuildId, [(1, -5), (2, -3)]);

        var counts = _store.GetSqliteCounts(GuildId);

        Assert.Equal(0, counts[1]);
        Assert.Equal(0, counts[2]);
    }

    [Fact]
    public void WriteToSqlite_KeepsExistingSchemaReadable()
    {
        // 舊程式建表的方式，確認新的 upsert 和讀取都能用在既有的 db 檔
        using (var cn = new SqliteConnection("Data Source=" + _dbPath))
        {
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS \"{GuildId}\" (\"EmoteID\" BIGINT, \"ActivityNum\" INT, PRIMARY KEY(\"EmoteID\"));" +
                $"INSERT OR REPLACE INTO `{GuildId}` VALUES (10, 100);";
            cmd.ExecuteNonQuery();
        }

        _store.WriteToSqlite(GuildId, [(10, 1)]);

        Assert.Equal(101, _store.GetSqliteCounts(GuildId)[10]);
    }
}
