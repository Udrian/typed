using System.Collections.Concurrent;
using TypeD.Helpers;

namespace TypeDTest;

public class CMDTests
{
    [Fact(Timeout = 30000)]
    public async Task Run_SingleCommandReportsOutput()
    {
        var marker = $"cmd-output-{Guid.NewGuid():N}";
        var output = new ConcurrentQueue<string>();
        CMD.Output += output.Enqueue;
        try
        {
            await CMD.Run($"echo '{marker}'");

            Assert.Contains(marker, output);
        }
        finally
        {
            CMD.Output -= output.Enqueue;
        }
    }

    [Fact(Timeout = 30000)]
    public async Task Run_CommandsShareShellStateAndReportErrors()
    {
        var marker = $"cmd-state-{Guid.NewGuid():N}";
        var errorMarker = $"cmd-error-{Guid.NewGuid():N}";
        var output = new ConcurrentQueue<string>();
        CMD.Output += output.Enqueue;
        try
        {
            await CMD.Run(OperatingSystem.IsWindows()
                ? new[]
                {
                    $"$cmdTestValue = '{marker}'",
                    "echo $cmdTestValue",
                    $"[Console]::Error.WriteLine('{errorMarker}')"
                }
                : new[]
                {
                    $"cmdTestValue='{marker}'",
                    "echo \"$cmdTestValue\"",
                    $"echo '{errorMarker}' >&2"
                });

            Assert.Contains(marker, output);
            Assert.Contains($"[ERROR] {errorMarker}", output);
        }
        finally
        {
            CMD.Output -= output.Enqueue;
        }
    }
}
