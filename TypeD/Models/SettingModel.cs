using TypeD.Helpers;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Data.SaveContexts;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class SettingModel : ISettingModel
    {
        // Data
        Dictionary<SettingLevel, List<SettingContext>> Contexts { get; set; }

        // Model
        ISaveModel SaveModel { get; set; }
        IHookModel HookModel { get; set; }
        private readonly ConditionalWeakTable<ISetting, SettingLocation> Locations = new();
        private sealed record SettingLocation(Type ContextType, string Name);

        private static IEnumerable<PropertyInfo> SettingProperties(Type type) =>
            type.GetProperties().Where(property => typeof(ISetting).IsAssignableFrom(property.PropertyType));

        private static string Snapshot(ISetting setting) =>
            JsonSerializer.Serialize(setting.GetValue(), setting.GetValueType(), JSON.CreateOptions());

        private static object CopyValue(ISetting setting) =>
            JsonSerializer.Deserialize(Snapshot(setting), setting.GetValueType(), JSON.CreateOptions());

        private SettingContext Raw(Type type, SettingLevel level) => Contexts[level].First(c => c.GetType() == type);

        private static void MakeExplicit(SettingContext context)
        {
            context.Overrides ??= SettingProperties(context.GetType())
                .Where(property => property.GetValue(context) is ISetting setting &&
                    !Equals(setting.GetValue(), setting.GetDefaultValue()))
                .Select(property => property.Name).ToHashSet();
        }

        private SettingLocation Locate(ISetting setting) =>
            Locations.TryGetValue(setting, out var location) ? location :
                throw new InvalidOperationException("The setting is not associated with a loaded setting context.");

        private ISetting GetSetting(SettingLocation location, SettingLevel level) =>
            (ISetting)location.ContextType.GetProperty(location.Name).GetValue(Raw(location.ContextType, level));

        public bool HasProjectDefault(ISetting setting, string entry = null)
            => HasOverride(setting, SettingLevel.Project, entry);

        public bool HasUserOverride(ISetting setting, string entry = null)
            => HasOverride(setting, SettingLevel.User, entry);

        private bool HasOverride(ISetting setting, SettingLevel level, string entry)
        {
            if (!Locations.TryGetValue(setting, out var location))
                return false;
            var context = Raw(location.ContextType, level);
            MakeExplicit(context);
            return context.Overrides.Contains(location.Name) &&
                (entry == null || GetSetting(location, level).GetValue() is IDictionary values && values.Contains(entry));
        }

        public object GetRevertValue(ISetting setting, string entry = null, object fallback = null)
        {
            if (!HasProjectDefault(setting, entry))
            {
                if (!Locations.TryGetValue(setting, out var location))
                    return entry == null ? setting.GetDefaultValue() : fallback;
                var system = GetSetting(location, SettingLevel.System);
                var systemValue = CopyValue(system);
                return entry == null ? systemValue :
                    systemValue is IDictionary entries && entries.Contains(entry) ? entries[entry] : fallback;
            }
            var value = CopyValue(GetSetting(Locate(setting), SettingLevel.Project));
            return entry == null ? value : ((IDictionary)value)[entry];
        }

        public async Task SetProjectDefaultAsync(Project project, ISetting setting, object value, bool remove = false, string entry = null)
        {
            ArgumentNullException.ThrowIfNull(project);
            var location = Locate(setting);
            var context = Raw(location.ContextType, SettingLevel.Project);
            MakeExplicit(context);
            var stored = GetSetting(location, SettingLevel.Project);
            if (entry != null && stored.GetValue() is not IDictionary)
                throw new InvalidOperationException("Individual defaults require a dictionary setting.");
            if (entry == null)
            {
                stored.SetValue(remove ? stored.GetDefaultValue() :
                    JsonSerializer.Deserialize(JsonSerializer.Serialize(value, setting.GetValueType(), JSON.CreateOptions()),
                        setting.GetValueType(), JSON.CreateOptions()));
                if (remove) context.Overrides.Remove(location.Name);
                else context.Overrides.Add(location.Name);
            }
            else
            {
                var values = (IDictionary)CopyValue(stored);
                if (remove) values.Remove(entry);
                else values[entry] = value;
                stored.SetValue(values);
                if (values.Count == 0) context.Overrides.Remove(location.Name);
                else context.Overrides.Add(location.Name);
            }
            context.ShouldSave = true;
            var save = new SettingSaveContext();
            save.Init(ResourceModel);
            save.Settings.Add(context);
            await save.SaveAction(project);
        }

        private IResourceModel ResourceModel;

        public void ClearUserOverride(ISetting setting, string entry = null)
        {
            var location = Locate(setting);
            var context = Raw(location.ContextType, SettingLevel.User);
            MakeExplicit(context);
            var stored = GetSetting(location, SettingLevel.User);
            if (entry != null && stored.GetValue() is not IDictionary)
                throw new InvalidOperationException("Individual overrides require a dictionary setting.");
            if (entry == null)
            {
                context.Overrides.Remove(location.Name);
                stored.SetValue(stored.GetDefaultValue());
            }
            else
            {
                var values = (IDictionary)CopyValue(stored);
                values.Remove(entry);
                stored.SetValue(values);
                if (values.Count == 0) context.Overrides.Remove(location.Name);
            }
            QueueSave(context);
        }

        // Properties
        private string SettingsPath { get { return "TypeO/Settings"; } }

        // Constructors
        public SettingModel()
        {
            Contexts = new Dictionary<SettingLevel, List<SettingContext>>();
            Contexts.Add(SettingLevel.System, new List<SettingContext>());
            Contexts.Add(SettingLevel.Project, new List<SettingContext>());
            Contexts.Add(SettingLevel.User, new List<SettingContext>());
        }

        public void Init(IResourceModel resourceModel)
        {
            ResourceModel = resourceModel;
            SaveModel = resourceModel.Get<ISaveModel>();
            HookModel = resourceModel.Get<IHookModel>();

            HookModel.AddHook<ExitHook>((hook) =>
            {
                var settingSaveContext = SaveModel.GetSaveContext<SettingSaveContext>();
                settingSaveContext.Settings.Clear();

                foreach (var contexts in Contexts.Values)
                {
                    foreach(var context in contexts)
                    {
                        if (context.SaveOnExit && context.ShouldSave)
                        {
                            settingSaveContext.Settings.Add(context);
                        }
                    }
                }

                SaveModel.SaveNow<SettingSaveContext>(hook.Project);
            });
        }

        // Functions
        public void InitContext<T>(Project project) where T : SettingContext, new()
        {
            RemoveContext<T>();
            var paths = new List<string>() { GetPath(project, SettingLevel.System), project == null ? null : GetPath(project, SettingLevel.Project), project == null ? null : GetPath(project, SettingLevel.User) };
            var i = 0;
            foreach(var path in paths)
            {
                var filePath = $"{path}/{GetName<T>()}.json";
                T context = null;
                if (path != null && File.Exists(filePath))
                {
                    var loaded = JSON.Deserialize<T>(filePath);
                    context = new T().Merge(loaded) as T;
                    context.Overrides = loaded.Overrides;
                }
                else
                {
                    context = new T();
                }
                context.Init((SettingLevel)i);
                Contexts[(SettingLevel)i].Add(context);
                i++;
            }
        }

        public void RemoveContext<T>() where T : SettingContext, new()
        {
            foreach(SettingLevel level in Enum.GetValues(typeof(SettingLevel)))
            {
                Contexts[level].RemoveAll(s => s.GetType() == typeof(T));
            }
        }

        public T GetContext<T>(SettingLevel settingLevel = SettingLevel.User) where T : SettingContext, new()
        {
            if (!Contexts[SettingLevel.System].Any(c => c.GetType() == typeof(T))) return null;
            T retVal = new T();

            for(int i = 0; i <= (int)settingLevel; i++)
            {
                retVal = retVal.Merge(Contexts[(SettingLevel)i].FirstOrDefault(c => c.GetType() == typeof(T)) as T) as T;
            }

            retVal.Level = settingLevel;
            // Dictionary settings (notably shortcuts) inherit individual entries across scopes.
            foreach (var property in SettingProperties(typeof(T)))
            {
                var setting = (ISetting)property.GetValue(retVal);
                if (setting.GetValue() is IDictionary)
                {
                    var values = (IDictionary)Activator.CreateInstance(setting.GetValueType());
                    for (int i = 0; i <= (int)settingLevel; i++)
                    {
                        var raw = Raw(typeof(T), (SettingLevel)i);
                        if (raw.Overrides != null && !raw.Overrides.Contains(property.Name)) continue;
                        if (((ISetting)property.GetValue(raw)).GetValue() is IDictionary entries)
                            foreach (DictionaryEntry entry in entries) values[entry.Key] = entry.Value;
                    }
                    setting.SetValue(values);
                }
                else setting.SetValue(CopyValue(setting));
                Locations.Add(setting, new SettingLocation(typeof(T), property.Name));
            }
            retVal.OriginalValues = SettingProperties(typeof(T)).ToDictionary(p => p.Name, p => Snapshot((ISetting)p.GetValue(retVal)));
            return retVal;
        }

        public void SetContext(SettingContext settingContext, SettingLevel? settingLevel = null)
        {
            if(settingLevel.HasValue)
            {
                settingContext.Init(settingLevel.Value);
            }
            if (settingContext.OriginalValues != null)
            {
                var stored = Raw(settingContext.GetType(), settingContext.Level);
                MakeExplicit(stored);
                foreach (var property in SettingProperties(settingContext.GetType()))
                {
                    var setting = (ISetting)property.GetValue(settingContext);
                    var value = Snapshot(setting);
                    if (value == settingContext.OriginalValues[property.Name]) continue;
                    var destination = (ISetting)property.GetValue(stored);
                    if (setting.GetValue() is IDictionary updated)
                    {
                        var original = (IDictionary)JsonSerializer.Deserialize(settingContext.OriginalValues[property.Name], setting.GetValueType(), JSON.CreateOptions());
                        var values = (IDictionary)CopyValue(destination);
                        foreach (var key in original.Keys)
                            if (!updated.Contains(key)) values.Remove(key);
                        foreach (DictionaryEntry entry in updated)
                            if (!original.Contains(entry.Key) || !Equals(original[entry.Key], entry.Value)) values[entry.Key] = entry.Value;
                        destination.SetValue(values);
                        if (values.Count == 0) stored.Overrides.Remove(property.Name);
                        else stored.Overrides.Add(property.Name);
                    }
                    else
                    {
                        destination.SetValue(CopyValue(setting));
                        stored.Overrides.Add(property.Name);
                    }
                    settingContext.OriginalValues[property.Name] = value;
                }
                QueueSave(stored);
                return;
            }
            var i = Contexts[settingContext.Level].FindIndex(s => s.GetType() == settingContext.GetType());
            Contexts[settingContext.Level][i] = settingContext;
            QueueSave(settingContext);
        }

        private void QueueSave(SettingContext settingContext)
        {
            settingContext.ShouldSave = true;

            if (!settingContext.SaveOnExit)
            {
                var settingSaveContext = SaveModel.GetSaveContext<SettingSaveContext>();
                settingSaveContext.Settings.Add(settingContext);
                SaveModel.AddSave<SettingSaveContext>();
            }
        }

        public string GetName<T>() where T : SettingContext, new()
        {
            return GetName(typeof(T));
        }

        public string GetName(Type type)
        {
            var attribute = Attribute.GetCustomAttribute(type, typeof(NameAttribute)) as NameAttribute;
            if (attribute == null)
            {
                var name = type.Name;
                foreach (var item in new List<string>() { "SettingContext", "Setting", "Context" })
                {
                    if (name.EndsWith(item))
                    {
                        name = name.Substring(0, name.LastIndexOf(item));
                        break;
                    }
                }
                return name;
            }

            return attribute.Name;
        }

        public string GetPath(Project project, SettingLevel settingLevel)
        {
            if(settingLevel == SettingLevel.System)
                return $"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}/{SettingsPath}";
            if(settingLevel == SettingLevel.Project)
                return $"{project.Location}/{SettingsPath}";
            if (settingLevel == SettingLevel.User)
                return $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/{SettingsPath}/{project.ProjectName}";
            return "";
        }
    }
}
