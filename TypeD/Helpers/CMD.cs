using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TypeD.Helpers
{
    // TODO: Make this into a Model instead
    public static class CMD
    {
        public static event Action<string> Output;

        public static async Task Run(string cmd)
        {
            await Run(new string[] { cmd });
        }

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
                        Output?.Invoke(line);
                }
            }
        }
    }
}
