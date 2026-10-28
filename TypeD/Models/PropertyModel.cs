using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class PropertyModel : IPropertyModel
    {
        private Dictionary<Type, IPropertyDefinition> Definitions { get; } = new();

        public void Init(IResourceModel resourceModel)
        {
        }

        public void Register(IPropertyDefinition definition)
        {
            Unregister(definition.Type);
            Definitions[definition.Type] = definition;
            if (definition.JsonConverter != null)
                JSON.AddConverter(definition.JsonConverter);
        }

        public void Unregister(Type type)
        {
            if (!Definitions.Remove(type, out var definition))
                return;
            if (definition.JsonConverter != null)
                JSON.RemoveConverter(definition.JsonConverter);
        }

        public IPropertyDefinition Get(Type type)
        {
            if (type == null)
                return null;
            if (Definitions.TryGetValue(type, out var definition))
                return definition;
            // A definition registered for System.Enum handles every enum type without its own definition.
            if (type.IsEnum && Definitions.TryGetValue(typeof(Enum), out definition))
                return definition;
            return null;
        }

        public IReadOnlyList<IPropertyDefinition> List()
        {
            return Definitions.Values.ToList();
        }
    }
}
