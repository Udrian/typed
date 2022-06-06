using System;
using System.Collections.Generic;
using TypeD.Components;

namespace TypeD.Models.Data
{
    public class Component
    {
        // Properties
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public ComponentTemplate Template { get; set; }
        public string FullName { get { return $"{Namespace}.{ClassName}"; } }
        public Component ParentComponent { get; set; }
        public List<Type> Interfaces { get; set; }
        public Type TypeOBaseType { get; set; }
        public List<Component> Children { get; set; }

        public Component()
        {
            Interfaces = new List<Type>();
            Children = new List<Component>();
        }
    }
}
