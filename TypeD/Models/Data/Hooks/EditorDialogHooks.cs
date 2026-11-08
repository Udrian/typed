namespace TypeD.Models.Data.Hooks
{
    /// <summary>Requests a module-specific component creation dialog.</summary>
    public class CreateComponentDialogHook : Hook
    {
        /// <summary>Gets or sets the project being edited.</summary>
        public Project Project { get; set; }
        /// <summary>Gets or sets the suggested component namespace.</summary>
        public string ComponentNamespace { get; set; }
        /// <summary>Gets or sets the component type the dialog should create.</summary>
        public Component BaseComponent { get; set; }
        /// <summary>Gets or sets the asynchronous dialog result supplied by a module.</summary>
        public Task<ComponentCreationOptions> Result { get; set; }
    }

    /// <summary>Requests a module-specific custom-property dialog.</summary>
    public class AddCustomPropertyDialogHook : Hook
    {
        /// <summary>Gets or sets the registered property definitions.</summary>
        public IEnumerable<IPropertyDefinition> Definitions { get; set; }
        /// <summary>Gets or sets property names that are already in use.</summary>
        public IEnumerable<string> TakenNames { get; set; }
        /// <summary>Gets or sets the asynchronous dialog result supplied by a module.</summary>
        public Task<(CustomProperty Property, Type Type)?> Result { get; set; }
    }
}