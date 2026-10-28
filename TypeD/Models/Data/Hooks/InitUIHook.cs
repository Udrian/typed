using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    public class InitUIHook : Hook
    {
        public Project Project { get; private set; }
        public Menu Menu { get; set; }
        public Toolbar Toolbar { get; set; }

        public InitUIHook() { }
        public InitUIHook(Project project)
        {
            Menu = new Menu();
            Toolbar = new Toolbar();
            Project = project;
        }
    }
}
