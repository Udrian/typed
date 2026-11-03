using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    /// <summary>
    /// Fired while the user interface is initialized, so listeners can add menu and toolbar items.
    /// </summary>
    public class InitUIHook : Hook
    {
        /// <summary>Gets the project the UI is initialized for.</summary>
        public Project Project { get; private set; }
        /// <summary>Gets or sets the main menu that listeners add items to.</summary>
        public Menu Menu { get; set; }
        /// <summary>Gets or sets the toolbar that listeners add items to.</summary>
        public Toolbar Toolbar { get; set; }

        /// <summary>Initializes a new instance of the <see cref="InitUIHook"/> class.</summary>
        public InitUIHook() { }

        /// <summary>Initializes a new instance of the <see cref="InitUIHook"/> class with an empty menu and toolbar.</summary>
        /// <param name="project">The project the UI is initialized for.</param>
        public InitUIHook(Project project)
        {
            Menu = new Menu();
            Toolbar = new Toolbar();
            Project = project;
        }
    }
}
