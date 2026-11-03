using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TypeD.Helpers
{
    // TODO: Make this into a Model instead
    /// <summary>
    /// Runs PowerShell commands and reports their output.
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

        /// <summary>Runs a single command in PowerShell.</summary>
        /// <param name="cmd">The command.</param>
        public static async Task Run(string cmd)
        {
            await Run(new string[] { cmd });
        }

        /// <summary>Runs commands in sequence in one PowerShell session and waits for it to exit.</summary>
        /// <param name="cmds">The commands.</param>
        public static async Task Run(string[] cmds)
        {
            var process = new Process();
            var info = new ProcessStartInfo();
            info.FileName = "powershell.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;

            process.StartInfo = info;
            process.Start();

            var processTask = new Task(() =>
            {
                using (StreamWriter sw = process.StandardInput)
                using (StreamReader sr = process.StandardOutput)
                {
                    foreach (var cmd in cmds) {
                        WriteStream(sw, cmd);
                    }
                    WriteStream(sw, "exit");

                    ReadStream(sr);
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

        private static void ReadStream(StreamReader sr)
        {
            if (sr.BaseStream.CanRead)
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if(line != null)
                        ReportOutput(line);
                }
            }
        }
    }
}
