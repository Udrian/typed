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
            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                if (Definitions.TryGetValue(baseType, out definition))
                    return definition;
            }
            return null;
        }

        public IReadOnlyList<IPropertyDefinition> List()
        {
            return Definitions.Values.ToList();
        }
    }
}
