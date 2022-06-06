using TypeD.View;

namespace TypeD.Models.Data.Hooks
{
    public class InitUIHook : Hook
    {
        public Project Project { get; private set; }
        public Menu Menu { get; set; }

        public InitUIHook() { }
        public InitUIHook(Project project)
        {
            Menu = new Menu();
            Project = project;
        }
    }
}
