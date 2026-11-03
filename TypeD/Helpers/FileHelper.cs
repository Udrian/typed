using Avalonia.Platform.Storage;

namespace TypeD.Helpers
{
    /// <summary>
    /// Helpers for locating and reading files.
    /// </summary>
    public static class FileHelper
    {
        /// <summary>Gets the file picker filter for solution files.</summary>
        public static FilePickerFileType SolutionFileType { get; } = new("Solution Files")
        {
            Patterns = ["*.sln"],
            MimeTypes = ["5application/octet-stream"]
        };

        /// <summary>Gets the file picker filter for TypeO project files.</summary>
        public static FilePickerFileType TypeOProjectFileType { get; } = new("TypeO Projects")
        {
            Patterns = ["*.typeo"],
            MimeTypes = ["5application/octet-stream"]
        };

        /// <summary>Reads delimited values from the lines between a start and an end block line of a file.</summary>
        /// <param name="filePath">The file to read.</param>
        /// <param name="startBlock">Text that the line starting the block ends with.</param>
        /// <param name="endBlock">Text that the line ending the block ends with.</param>
        /// <param name="startDelimiter">The text before the value on each line.</param>
        /// <param name="endDelimiter">The text after the value on each line.</param>
        /// <returns>The values, or an empty list if the file does not exist.</returns>
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
