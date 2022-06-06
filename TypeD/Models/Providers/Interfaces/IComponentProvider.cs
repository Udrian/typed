using System;
using System.Collections.Generic;
using TypeD.Components;
using TypeD.Models.Data;

namespace TypeD.Models.Providers.Interfaces
{
    public interface IComponentProvider : IProvider
    {
        public T Create<T>(Project project, string className, string @namespace, Component parentComponent = null, List<string> interfaces = null) where T : ComponentTemplate;
        public void Save(Project project, Component component);
        public Component Load(Project project, string fullName);
        public void Delete(Project project, Component component);
        public void Rename(Project project, Component component, string newClassName);
        public bool Exists(Project project, Component component);
        public bool Exists(Project project, Type type);
        public List<Component> ListAll(Project project);
        public string GetPath(Project project, Component component);
        public string GetPath(Project project, string fullName);
    }
}
