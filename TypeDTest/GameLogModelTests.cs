using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeDTest;

public class GameLogModelTests
{
    [Fact]
    public void Add_StoresEntriesAndRaisesEvent()
    {
        var model = CreateModel();
        GameLogEntry added = null;
        model.EntryAdded += entry => added = entry;

        model.Add(GameLogLevel.Warning, "watch out");

        var entry = Assert.Single(model.GetEntries());
        Assert.Same(entry, added);
        Assert.Equal(GameLogLevel.Warning, entry.Level);
        Assert.Equal("watch out", entry.Message);
    }

    [Fact]
    public void AddRange_StoresEntriesAndRaisesOneBatchEvent()
    {
        var model = CreateModel();
        var addedEntryCount = 0;
        IReadOnlyList<GameLogEntry> addedBatch = null;
        var trimmed = true;
        model.EntryAdded += _ => addedEntryCount++;
        model.EntriesAdded += (entries, entriesWereTrimmed) =>
        {
            addedBatch = entries;
            trimmed = entriesWereTrimmed;
        };

        model.AddRange(new[]
        {
            new GameLogEntry { Time = DateTime.Now, Level = GameLogLevel.Info, Message = "first" },
            new GameLogEntry { Time = DateTime.Now, Level = GameLogLevel.Warning, Message = "second" }
        });

        Assert.Equal(2, addedEntryCount);
        Assert.Equal(new[] { "first", "second" }, addedBatch.Select(entry => entry.Message));
        Assert.False(trimmed);
        Assert.Equal(new[] { "first", "second" }, model.GetEntries().Select(entry => entry.Message));
    }

    [Fact]
    public void AddRange_ReportsWhenTheEntryLimitTrimsOldMessages()
    {
        var model = CreateModel();
        var trimmed = false;
        model.EntriesAdded += (_, entriesWereTrimmed) => trimmed = entriesWereTrimmed;
        var entries = Enumerable.Range(0, 5100)
            .Select(i => new GameLogEntry { Time = DateTime.Now, Level = GameLogLevel.Debug, Message = $"line {i}" })
            .ToArray();

        model.AddRange(entries);

        var storedEntries = model.GetEntries();
        Assert.True(trimmed);
        Assert.Equal(5000, storedEntries.Count);
        Assert.Equal("line 100", storedEntries[0].Message);
        Assert.Equal("line 5099", storedEntries[^1].Message);
    }

    [Fact]
    public void Clear_RemovesEntriesAndRaisesEvent()
    {
        var model = CreateModel();
        model.Add(GameLogLevel.Info, "hello");
        var cleared = false;
        model.Cleared += () => cleared = true;

        model.Clear();

        Assert.Empty(model.GetEntries());
        Assert.True(cleared);
    }

    [Fact]
    public void GetText_FormatsEntriesWithLevelTags()
    {
        var model = CreateModel();
        model.Add(GameLogLevel.Error, "boom");
        model.Add(GameLogLevel.Ludacris, "verbose");

        var text = model.GetText();

        Assert.Contains("EROR: boom", text);
        Assert.Contains("LDCR: verbose", text);
    }

    [Fact]
    public void Add_CapsStoredEntries()
    {
        var model = CreateModel();

        for (var i = 0; i < 5100; i++)
            model.Add(GameLogLevel.Debug, $"line {i}");

        var entries = model.GetEntries();
        Assert.Equal(5000, entries.Count);
        Assert.Equal("line 100", entries[0].Message);
        Assert.Equal("line 5099", entries[^1].Message);
    }

    [Fact]
    public void SetLevelColor_UpdatesColorAndRaisesEvent()
    {
        var model = CreateModel();
        var changes = 0;
        model.LevelColorsChanged += () => changes++;

        model.SetLevelColor(GameLogLevel.Fatal, "#123456");

        Assert.Equal("#123456", model.GetLevelColor(GameLogLevel.Fatal));
        Assert.Equal(1, changes);

        // Setting the same color again or an empty color changes nothing
        model.SetLevelColor(GameLogLevel.Fatal, "#123456");
        model.SetLevelColor(GameLogLevel.Fatal, "");
        Assert.Equal("#123456", model.GetLevelColor(GameLogLevel.Fatal));
        Assert.Equal(1, changes);
    }

    [Fact]
    public void CrashReport_IncludesGameLog()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var crash = resources.Get<ICrashModel>();
        crash.Init(resources);
        var gameLog = resources.Get<IGameLogModel>();
        gameLog.Add(GameLogLevel.Fatal, "the game died");
        crash.CurrentProject = CrashModelTests.CreateProject(typeof(GameLogModelTests).Assembly.GetName().Name);

        var report = crash.Identify(Thrown());

        Assert.NotNull(report);
        Assert.Contains("FATL: the game died", report.GameLog);
    }

    private static IBatchedGameLogModel CreateModel()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        var model = Assert.IsAssignableFrom<IBatchedGameLogModel>(resources.Get<IGameLogModel>());
        model.Init(resources);
        return model;
    }

    private static Exception Thrown()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
