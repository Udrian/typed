using System.Collections.ObjectModel;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeD.Models.Providers
{
    internal class RecentProvider : IRecentProvider
    {
        public ObservableCollection<Recent> Recents { get; set; }

        // Constructors
        public RecentProvider()
        {
            Recents = new ObservableCollection<Recent>();
        }

        public void Init(IResourceModel resourceModel)
        {
            Load();
        }

        // Functions
        public void Add(string projectFilePath, string projectName)
        {
            for(int i = Recents.Count - 1; i >= 0; i--)
            {
                var recent = Recents[i];
                if(PathsEqual(recent.Path, projectFilePath) || !File.Exists(recent.Path))
                {
                    Recents.RemoveAt(i);
                }
            }
            Recents.Insert(0, new Recent() { Name = projectName, Path = projectFilePath, DateTime = DateTime.Now });

            if (Recents.Count > RecentLength)
            {
                for(int i = RecentLength; i < Recents.Count - RecentLength; i++)
                {
                    Recents.RemoveAt(i);
                }
            }

            Save();
        }

        public void Rename(string oldProjectFilePath, string newProjectFilePath, string projectName)
        {
            var matches = Recents.Where(recent => PathsEqual(recent.Path, oldProjectFilePath)).ToList();
            if (matches.Count == 0) return;

            var original = Recents.ToList();
            var dateTime = matches.Max(recent => recent.DateTime);
            try
            {
                for (var i = Recents.Count - 1; i >= 0; i--)
                    if (PathsEqual(Recents[i].Path, oldProjectFilePath) ||
                        PathsEqual(Recents[i].Path, newProjectFilePath))
                        Recents.RemoveAt(i);
                var recent = new Recent
                {
                    Name = projectName,
                    Path = newProjectFilePath,
                    DateTime = dateTime
                };
                var index = Recents.TakeWhile(entry => entry.DateTime >= dateTime).Count();
                Recents.Insert(index, recent);
                Trim();
                Save();
            }
            catch
            {
                Recents.Clear();
                foreach (var recent in original)
                    Recents.Add(recent);
                throw;
            }
        }

        public void Remove(string projectFilePath)
        {
            var original = Recents.ToList();
            var changed = false;
            for (var i = Recents.Count - 1; i >= 0; i--)
            {
                if (!PathsEqual(Recents[i].Path, projectFilePath)) continue;
                Recents.RemoveAt(i);
                changed = true;
            }
            if (!changed) return;
            try
            {
                Save();
            }
            catch
            {
                Recents.Clear();
                foreach (var recent in original)
                    Recents.Add(recent);
                throw;
            }
        }

        public IEnumerable<Recent> Get()
        {
            return Recents;
        }

        // Internal
        private string RecentFilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TypeO", "recent"); } }
        private int RecentLength { get { return 5; } }

        private void Load()
        {
            Recents.Clear();
            var recents = JSON.Deserialize<List<Recent>>(RecentFilePath) ?? new List<Recent>();
            recents.RemoveAll((recent) => { return !File.Exists(recent.Path); });

            recents.ForEach(x => Recents.Add(x));
        }

        private void Save()
        {
            JSON.Serialize(Recents, RecentFilePath);
        }

        private void Trim()
        {
            while (Recents.Count > RecentLength)
                Recents.RemoveAt(Recents.Count - 1);
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            try
            {
                return comparer.Equals(Path.GetFullPath(left), Path.GetFullPath(right));
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return comparer.Equals(left, right);
            }
        }
    }
}
