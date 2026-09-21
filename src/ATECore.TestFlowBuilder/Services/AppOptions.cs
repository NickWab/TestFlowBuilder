namespace ATECore.TestFlowBuilder.Services;

/// <summary>
/// Startup options. Defaults match the design's props; each can be overridden on the command line:
/// <c>--sample-flow</c>, <c>--auto-run</c>, <c>--project "Name"</c>.
/// </summary>
public sealed record AppOptions(bool StartWithSampleFlow = false, bool AutoRunOnDrop = false, string ProjectName = "RF-Module Rev C2")
{
    public static AppOptions Parse(string[] args)
    {
        var options = new AppOptions();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--sample-flow": options = options with { StartWithSampleFlow = true }; break;
                case "--auto-run": options = options with { AutoRunOnDrop = true }; break;
                case "--project" when i + 1 < args.Length: options = options with { ProjectName = args[++i] }; break;
            }
        }
        return options;
    }
}
