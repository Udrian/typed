using System.Collections.Generic;
using System.Threading.Tasks;
using TypeD.Models.Data;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core.Entities.Drawables;

namespace TypeD.View.Viewer
{
    public class DrawableViewer
    {
        readonly TypeO FakeTypeO;

        private class FakeGame : Game
        {
            public override void Initialize()
            {
            }

            public override void Cleanup()
            {
            }

            public override void Draw()
            {
                foreach(var drawable in Drawables.Get<Drawable>())
                {
                    drawable.Draw(null);
                }
            }

            public override void Update(double dt)
            {
            }
        }

        private FakeGame Game { get; set; }

        public DrawableViewer(Project project, Component drawable, List<TypeOEngine.Typedeaf.Core.Engine.Module> modules = null)
        {
            if (drawable.TypeOBaseType != typeof(Drawable2d)) return;
            var typeInfo = project.Assembly.GetType(drawable.FullName);
            if (typeInfo == null) return;

            FakeTypeO = TypeO.Create<FakeGame>("Drawable Viewer") as TypeO;
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    FakeTypeO.LoadModule(module);
                }
            }
            var task = new Task(() =>
            {
                FakeTypeO.Start();
            });
            task.Start();

            while(!(FakeTypeO?.Context?.Game?.Initialized == true))
            {
                Task.Delay(100);
            }
            Game = FakeTypeO.Context.Game as FakeGame;
            Game.Drawables.Create(typeInfo);
        }

        public void Close()
        {
            Game.Exit();
        }
    }
}
