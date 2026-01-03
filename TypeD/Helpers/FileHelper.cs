using Avalonia.Platform.Storage;

namespace TypeD.Helpers
{
    public static class FileHelper
    {
        public static FilePickerFileType SolutionFileType { get; } = new("Solution Files")
        {
            Patterns = ["*.sln"],
            MimeTypes = ["5application/octet-stream"]
        };

        public static FilePickerFileType TypeOProjectFileType { get; } = new("TypeO Projects")
        {
            Patterns = ["*.typeo"],
            MimeTypes = ["5application/octet-stream"]
        };

        public static List<string> FetchStringList(string filePath, string startBlock, string endBlock, string startDelimiter = "<", string endDelimiter = ">")
        {
            var output = new List<string>();

            if (File.Exists(filePath))
            {
                var fileContent = File.ReadAllLines(filePath);

                var fetchLine = false;
                foreach (var line in fileContent)
                {
                    if (line.EndsWith(startBlock))
                    {
                        fetchLine = true;
                    }
                    else if (line.EndsWith(endBlock))
                    {
                        break;
                    }
                    else if (fetchLine)
                    {
                        int startIndex = line.IndexOf(startDelimiter) + startDelimiter.Length;
                        int endIndex = line.IndexOf(endDelimiter, startIndex);
                        output.Add(line[startIndex..endIndex]);
                    }
                }
            }

            return output;
        }
    }
}
