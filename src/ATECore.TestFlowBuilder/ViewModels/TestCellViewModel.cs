using System.Globalization;
using ATECore.TestFlowBuilder.Models;

namespace ATECore.TestFlowBuilder.ViewModels;

public enum CellStatus { Idle, Running, Pass }

/// <summary>One C# cell in the flow.</summary>
public sealed class TestCellViewModel : ObservableObject
{
    private string _number = "";
    private string _code;
    private CellStatus _status = CellStatus.Idle;
    private bool _isExpanded = true;
    private bool _resultsExpanded = true;
    private double? _duration;

    public TestCellViewModel(TestDefinition definition)
    {
        Definition = definition;
        _code = definition.Code;
    }

    public TestDefinition Definition { get; }
    public string Name => Definition.Name;
    public string Driver => Definition.Driver;
    public IReadOnlyList<Measurement> Measurements => Definition.Measurements;
    public IReadOnlyList<AssertionResult> Assertions => Definition.Assertions;
    public string Log => Definition.Log;

    public string Number { get => _number; set => Set(ref _number, value); }

    public string Code
    {
        get => _code;
        set
        {
            if (!Set(ref _code, value)) return;
            OnPropertyChanged(nameof(LineCount));
            OnPropertyChanged(nameof(EditorHeight));
            OnPropertyChanged(nameof(Meta));
        }
    }

    public CellStatus Status
    {
        get => _status;
        set
        {
            if (!Set(ref _status, value)) return;
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(RunLabel));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsPassed));
            OnPropertyChanged(nameof(DurationLabel));
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (!Set(ref _isExpanded, value)) return;
            OnPropertyChanged(nameof(ToggleGlyph));
            OnPropertyChanged(nameof(ToggleTip));
        }
    }

    /// <summary>Results (measurements + console) can be hidden on their own while the code stays open.</summary>
    public bool ResultsExpanded
    {
        get => _resultsExpanded;
        set
        {
            if (!Set(ref _resultsExpanded, value)) return;
            OnPropertyChanged(nameof(ResultsToggleLabel));
        }
    }

    public string ToggleGlyph => IsExpanded ? "−" : "+";
    public string ToggleTip => IsExpanded ? "Minimize" : "Expand";
    public string ResultsToggleLabel => ResultsExpanded ? "HIDE RESULTS" : "SHOW RESULTS";

    public double? Duration
    {
        get => _duration;
        set { if (Set(ref _duration, value)) OnPropertyChanged(nameof(DurationLabel)); }
    }

    public bool IsRunning => Status == CellStatus.Running;
    public bool IsPassed => Status == CellStatus.Pass;

    public string StatusLabel => Status switch { CellStatus.Running => "RUNNING", CellStatus.Pass => "PASSED", _ => "NOT RUN" };
    public string RunLabel => IsRunning ? "Running" : "Run";
    public string DurationLabel => IsPassed && Duration is { } d ? d.ToString("0.00", CultureInfo.InvariantCulture) + " s" : "—";

    public int LineCount => Code.Split('\n').Length;
    /// <summary>Editor height in DIPs: one extra line of slack, capped at 22 lines (20 DIP per line + padding).</summary>
    public double EditorHeight => Math.Min(22, LineCount + 1) * 20 + 24;
    public string Meta => $"{Driver.Split('.')[^1]} · {LineCount} statements".ToUpperInvariant();
}
