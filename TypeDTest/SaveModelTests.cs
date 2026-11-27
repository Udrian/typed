using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using System.Reflection;
using System.Text.Json;

namespace TypeDTest;

public class SaveModelTests
{
    [Fact]
    public async Task ExclusiveUpdate_RejectsPendingChangesAndBlocksSaves()
    {
        var model = InitializeResources().Get<ISaveModel>();
        model.GetSaveContext<FirstContext>().Save = () => { };
        model.AddSave<FirstContext>();
        Assert.Throws<InvalidOperationException>(() => model.BeginExclusiveUpdate());
        await model.Save(null);

        var lease = model.BeginExclusiveUpdate();
        Assert.Throws<InvalidOperationException>(() => model.BeginExclusiveUpdate());
        var save = model.Save(null);
        Assert.False(save.IsCompleted);
        lease.Dispose();
        lease.Dispose();
        await save;
        using var next = model.BeginExclusiveUpdate();
    }

    [Fact]
    public async Task ExclusiveUpdate_BlocksImmediateSaves()
    {
        var model = InitializeResources().Get<ISaveModel>();
        var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        model.GetSaveContext<FirstContext>().Save = () => completed.SetResult(true);
        using (model.BeginExclusiveUpdate())
        {
            model.SaveNow<FirstContext>(null);
            Assert.False(completed.Task.IsCompleted);
        }
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExclusiveUpdate_BlocksAutosaveUntilReleased()
    {
        var model = InitializeResources().Get<ISaveModel>();
        var temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var projectPath = Path.Combine(temporaryRoot, "project");
        var snapshotPath = Path.Combine(temporaryRoot, "snapshot");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "saved.txt"), "saved");
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var projectData = JsonSerializer.Deserialize(
            """{"ProjectName":"Game","CSSolutionPath":"Game.sln","CSProjName":"Game"}""",
            constructor.GetParameters()[1].ParameterType);
        var project = (Project)constructor.Invoke(new[] { projectPath, projectData });
        try
        {
            var lease = model.BeginExclusiveUpdate();
            var autosave = model.CreateAutosave(project, snapshotPath);
            Assert.False(autosave.IsCompleted);
            Assert.False(Directory.Exists(snapshotPath));
            lease.Dispose();
            await autosave;
            Assert.Equal("saved", File.ReadAllText(Path.Combine(snapshotPath, "saved.txt")));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Save_ProcessesContextsAddedBySaveCallbacks()
    {
        var resources = InitializeResources();
        var model = resources.Get<ISaveModel>();
        var calls = new List<string>();
        model.GetSaveContext<FirstContext>().Save = () =>
        {
            calls.Add("first");
            model.GetSaveContext<SecondContext>().Save = () => calls.Add("second");
            model.AddSave<SecondContext>();
        };
        model.AddSave<FirstContext>();

        await model.Save(null);

        Assert.Equal(new[] { "first", "second" }, calls);
        Assert.False(model.AnythingToSave);
    }

    [Fact]
    public async Task Save_AllowsCallbacksToReadUnmodifiedContexts()
    {
        var model = InitializeResources().Get<ISaveModel>();
        model.GetSaveContext<FirstContext>().Save = () => model.GetSaveContext<SecondContext>();
        model.AddSave<FirstContext>();

        await model.Save(null);

        Assert.False(model.AnythingToSave);
    }

    [Fact]
    public async Task Save_PreservesPendingChangesWhenCallbackFails()
    {
        var model = InitializeResources().Get<ISaveModel>();
        var context = model.GetSaveContext<FirstContext>();
        context.Save = () => throw new InvalidOperationException("Save failed");
        model.AddSave<FirstContext>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => model.Save(null));

        Assert.True(model.AnythingToSave);
        context.Save = () => { };
        await model.Save(null);
        Assert.False(model.AnythingToSave);
    }

    [Fact]
    public async Task CreateAutosave_WritesPendingChangesToCopyAndKeepsThemPending()
    {
        var resources = InitializeResources();
        var model = resources.Get<ISaveModel>();
        var temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var projectPath = Path.Combine(temporaryRoot, "project");
        var snapshotPath = Path.Combine(temporaryRoot, "autosave");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "saved.txt"), "latest save");
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var projectData = JsonSerializer.Deserialize(
            """{"ProjectName":"Game","CSSolutionPath":"Game.sln","CSProjName":"Game"}""",
            constructor.GetParameters()[1].ParameterType);
        var project = (Project)constructor.Invoke(new[] { projectPath, projectData });
        model.GetSaveContext<AutosaveTestContext>().PendingValue = "unsaved edit";
        model.AddSave<AutosaveTestContext>();

        try
        {
            await model.CreateAutosave(project, snapshotPath);

            Assert.Equal("latest save", File.ReadAllText(Path.Combine(projectPath, "saved.txt")));
            Assert.Equal("latest save", File.ReadAllText(Path.Combine(snapshotPath, "saved.txt")));
            Assert.Equal("unsaved edit", File.ReadAllText(Path.Combine(snapshotPath, "pending.txt")));
            Assert.True(model.AnythingToSave);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static IResourceModel InitializeResources()
    {
        var resources = new TestResourceModel();
        TypeDInit.Init(resources);
        resources.Get<IUINotifyModel>().Init(resources);
        resources.Get<ISaveModel>().Init(resources);
        return resources;
    }

    public class FirstContext : SaveContext
    {
        public Action Save { get; set; }

        public override void Init(IResourceModel resourceModel, object param = null) { }

        public override Task SaveAction(Project project)
        {
            Save();
            return Task.CompletedTask;
        }
    }

    public class SecondContext : FirstContext { }

    public class AutosaveTestContext : SaveContext
    {
        public string PendingValue { get; set; }

        public override void Init(IResourceModel resourceModel, object param = null) { }

        public override Task SaveAction(Project project) => Task.CompletedTask;

        public override Task SaveAutosaveAction(Project project, Project snapshotProject)
        {
            File.WriteAllText(Path.Combine(snapshotProject.Location, "pending.txt"), PendingValue);
            return Task.CompletedTask;
        }
    }
}
