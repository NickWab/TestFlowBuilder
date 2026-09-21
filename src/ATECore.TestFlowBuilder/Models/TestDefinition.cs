namespace ATECore.TestFlowBuilder.Models;

public sealed record Measurement(string Label, string Value, string Spec);

public sealed record AssertionResult(string Mark, string Text);

/// <summary>A predefined test in the library: its C# source plus the canned bench result.</summary>
public sealed record TestDefinition(
    string Key,
    string Name,
    string Driver,
    string Blurb,
    string Code,
    IReadOnlyList<Measurement> Measurements,
    IReadOnlyList<AssertionResult> Assertions,
    string Log,
    double DurationSeconds);
