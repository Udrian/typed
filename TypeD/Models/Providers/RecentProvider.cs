using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
                if(recent.Path == projectFilePath || !File.Exists(recent.Path))
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
    }
}
