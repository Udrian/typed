using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TypeD.Helpers
{
    // TODO: Make this into a Model instead
    /// <summary>
    /// Runs commands in PowerShell on Windows or Bash on Unix and reports their output.
    /// </summary>
    public static class CMD
    {
        /// <summary>Occurs for each line of output produced by a command.</summary>
        public static event Action<string> Output;

        /// <summary>Raises <see cref="Output"/> with a line of output.</summary>
        /// <param name="output">The output line.</param>
        public static void ReportOutput(string output)
        {
            Output?.Invoke(output);
        }

        /// <summary>Runs a single command in the platform's shell.</summary>
        /// <param name="cmd">The command.</param>
        public static async Task Run(string cmd)
        {
            await Run(new string[] { cmd });
        }

        /// <summary>Runs commands in sequence in one shell session and waits for it to exit.</summary>
        /// <param name="cmds">The commands.</param>
        public static async Task Run(string[] cmds)
        {
            var process = new Process();
            var info = new ProcessStartInfo();
            info.FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "bash";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            process.StartInfo = info;
            process.Start();

            var processTask = new Task(() =>
            {
                using (StreamWriter sw = process.StandardInput)
                using (StreamReader sr = process.StandardOutput)
                using (StreamReader se = process.StandardError)
                {
                    foreach (var cmd in cmds) {
                        WriteStream(sw, cmd);
                    }
                    WriteStream(sw, "exit");

                    // Read output and error in parallel
                    var outputTask = Task.Run(() => ReadStream(sr, false));
                    var errorTask = Task.Run(() => ReadStream(se, true));
                    
                    Task.WaitAll(outputTask, errorTask);
                }
                process.WaitForExit();
            });
            processTask.Start();

            await processTask;
        }

        private static void WriteStream(StreamWriter sw, string cmd)
        {
            if (sw.BaseStream.CanWrite)
            {
                sw.WriteLine(cmd);
            }
        }

        private static void ReadStream(StreamReader sr, bool isError)
        {
            if (sr.BaseStream.CanRead)
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if(line != null)
                    {
                        if (isError)
                            ReportOutput($"[ERROR] {line}");
                        else
                            ReportOutput(line);
                    }
                }
            }
        }
    }
}
