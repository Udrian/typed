using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Models
{
    internal class IconModel : IIconModel
    {
        private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

        public string DefaultIconsPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "DefaultIcons");

        public void Init(IResourceModel resourceModel) { }

        public void Sync(Project project)
        {
            if (!Directory.Exists(DefaultIconsPath))
                return;

            Directory.CreateDirectory(project.ProjectIconsPath);
            foreach (var file in Directory.GetFiles(DefaultIconsPath))
            {
                var target = Path.Combine(project.ProjectIconsPath, Path.GetFileName(file));
                if (!File.Exists(target))
                    File.Copy(file, target);
            }
        }

        public List<string> List(Project project)
        {
            if (!Directory.Exists(project.ProjectIconsPath))
                return new List<string>();

            return Directory.GetFiles(project.ProjectIconsPath)
                .Where(IsImage)
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public string GetPath(Project project, string iconName)
        {
            foreach (var name in new[] { iconName, Component.DefaultIcon })
            {
                if (string.IsNullOrEmpty(name))
                    continue;

                var path = Path.Combine(project.ProjectIconsPath, Path.GetFileName(name));
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        public string Import(Project project, string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath) || !IsImage(sourceFilePath))
                throw new InvalidDataException($"'{sourceFilePath}' is not a supported image file.");

            Directory.CreateDirectory(project.ProjectIconsPath);
            var fileName = Path.GetFileName(sourceFilePath);
            var target = Path.Combine(project.ProjectIconsPath, fileName);
            if (!File.Exists(target))
            {
                File.Copy(sourceFilePath, target);
                return fileName;
            }

            if (File.ReadAllBytes(target).AsSpan().SequenceEqual(File.ReadAllBytes(sourceFilePath)))
                return fileName;

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            for (var i = 2; ; i++)
            {
                fileName = $"{name}{i}{extension}";
                target = Path.Combine(project.ProjectIconsPath, fileName);
                if (!File.Exists(target))
                {
                    File.Copy(sourceFilePath, target);
                    return fileName;
                }
            }
        }

        private static bool IsImage(string path)
        {
            return Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }
    }
}
