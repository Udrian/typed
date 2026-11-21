using TypeD.Models.Data;

/// <summary>
/// Represents the settings context for a module, including branches and repositories.
/// </summary>
public class ModuleSettingContext : SettingContext
{
    /// <summary>
    /// Gets or sets the list of branches for the module.
    /// </summary>
    public Setting<List<string>> Branches { get; set; } = new Setting<List<string>>(new List<string>());
    /// <summary>
    /// Gets or sets the list of repositories for the module.
    /// </summary>
    public Setting<List<string>> Repositories { get; set; } = new Setting<List<string>>(new List<string>());

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleSettingContext"/> class.
    /// </summary>
    public ModuleSettingContext() { SaveOnExit = true; }
}