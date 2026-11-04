using TypeD;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

public class SaveModelTests
{
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
}
