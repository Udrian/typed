
using TypeD.Models.Data;

namespace TypeD.View.Viewer
{
    public interface IViewer
    {
        public void Init(Project project, Component component);
        public Component Component { get; }
    }
}
