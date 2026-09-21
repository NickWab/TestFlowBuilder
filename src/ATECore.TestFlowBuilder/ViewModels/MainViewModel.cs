using System.Collections.ObjectModel;
using System.Windows.Input;
using ATECore.TestFlowBuilder.Models;
using ATECore.TestFlowBuilder.Services;

namespace ATECore.TestFlowBuilder.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private enum BuildState { Idle, Building, Done }

    private readonly AppOptions _options;
    private readonly AssistantService _assistant = new();

    private string _draft = "";
    private bool _aiThinking;
    private bool _dropActive;
    private BuildState _buildState = BuildState.Idle;
    private double _buildPercent;
    private string _buildMessage = "Ready. 0 cells compiled.";

    public MainViewModel(AppOptions options)
    {
        _options = options;
        Cells.CollectionChanged += (_, _) => OnCellsChanged();

        Chat.Add(new ChatMessageViewModel("Assistant",
            "Bench is online and Instrument.Hal.dll is loaded. Drag tests in from the library, or tell me what to measure and I will write the cell.", false));

        if (options.StartWithSampleFlow)
            foreach (var key in new[] { "psu_rail", "adc", "versions" }) AddCell(TestLibrary.Find(key)!);

        AddCommand = new RelayCommand(p => AddTest((string)p!));
        ToggleCommand = new RelayCommand(p => { var c = (TestCellViewModel)p!; c.IsExpanded = !c.IsExpanded; });
        ToggleResultsCommand = new RelayCommand(p => { var c = (TestCellViewModel)p!; c.ResultsExpanded = !c.ResultsExpanded; });
        RunCommand = new RelayCommand(p => _ = RunAsync((TestCellViewModel)p!));
        RemoveCommand = new RelayCommand(p => { Cells.Remove((TestCellViewModel)p!); BumpBuild(); });
        RunAllCommand = new RelayCommand(() => _ = RunAllAsync());
        ClearCommand = new RelayCommand(Clear);
        SendCommand = new RelayCommand(() => Send(Draft));
        SuggestionCommand = new RelayCommand(p => Send(Suggestions.First(s => s.Key == (string)p!).Label));
        BuildCommand = new RelayCommand(() => _ = BuildAsync());
    }

    public string ProjectName => _options.ProjectName;
    public IReadOnlyList<TestDefinition> Library => TestLibrary.All;

    public IReadOnlyList<SuggestionViewModel> Suggestions { get; } =
    [
        new("explain", "Explain the ADC test"),
        new("create", "Write a 1.8V rail test"),
        new("order", "Review flow order"),
    ];

    public ObservableCollection<TestCellViewModel> Cells { get; } = [];
    public ObservableCollection<ChatMessageViewModel> Chat { get; } = [];

    public ICommand AddCommand { get; }
    public ICommand ToggleCommand { get; }
    public ICommand ToggleResultsCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand RunAllCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand SendCommand { get; }
    public ICommand SuggestionCommand { get; }
    public ICommand BuildCommand { get; }

    public string Draft { get => _draft; set => Set(ref _draft, value); }
    public bool AiThinking { get => _aiThinking; private set => Set(ref _aiThinking, value); }
    public bool DropActive { get => _dropActive; set => Set(ref _dropActive, value); }

    public bool IsEmpty => Cells.Count == 0;
    public bool HasCells => Cells.Count > 0;

    public string FlowSummary
    {
        get
        {
            if (Cells.Count == 0) return "no cells yet";
            var passed = Cells.Count(c => c.IsPassed);
            return $"{Cells.Count} cells · {passed} passed · {Cells.Count - passed} not run";
        }
    }

    public string BuildLabel => _buildState switch { BuildState.Building => "Building…", BuildState.Done => "Rebuild", _ => "Build Tests" };
    public bool IsBuilding => _buildState == BuildState.Building;
    public double BuildPercent { get => _buildPercent; private set => Set(ref _buildPercent, value); }
    public string BuildMessage { get => _buildMessage; private set => Set(ref _buildMessage, value); }

    /// <summary>Raised after a cell is appended so the view can scroll it into view.</summary>
    public event Action<TestCellViewModel>? CellAdded;

    // ── flow ────────────────────────────────────────────────────────────────

    public void AddTest(string key)
    {
        DropActive = false;
        if (TestLibrary.Find(key) is not { } definition) return;
        var cell = AddCell(definition);
        if (_options.AutoRunOnDrop) _ = RunAfterAsync(cell, 120);
    }

    private TestCellViewModel AddCell(TestDefinition definition)
    {
        var cell = new TestCellViewModel(definition);
        Cells.Add(cell);
        CellAdded?.Invoke(cell);
        return cell;
    }

    private void OnCellsChanged()
    {
        for (var i = 0; i < Cells.Count; i++) Cells[i].Number = (i + 1).ToString("00");
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasCells));
        OnPropertyChanged(nameof(FlowSummary));
    }

    private void Clear()
    {
        Cells.Clear();
        _buildState = BuildState.Idle;
        BuildPercent = 0;
        BuildMessage = "Flow cleared.";
        NotifyBuildState();
    }

    // ── run ─────────────────────────────────────────────────────────────────

    private async Task RunAfterAsync(TestCellViewModel cell, int delayMs)
    {
        await Task.Delay(delayMs);
        await RunAsync(cell);
    }

    private async Task RunAllAsync()
    {
        var tasks = Cells.ToList().Select((c, i) => RunAfterAsync(c, i * 250));
        await Task.WhenAll(tasks);
    }

    private async Task RunAsync(TestCellViewModel cell)
    {
        if (cell.IsRunning || !Cells.Contains(cell)) return;
        cell.Status = CellStatus.Running;

        await Task.Delay((int)Math.Round(cell.Definition.DurationSeconds * 700));

        cell.ResultsExpanded = true;
        cell.Status = CellStatus.Pass;
        cell.Duration = cell.Definition.DurationSeconds;
        OnPropertyChanged(nameof(FlowSummary));
        BumpBuild();
    }

    // ── build ───────────────────────────────────────────────────────────────

    private void BumpBuild()
    {
        OnPropertyChanged(nameof(FlowSummary));
        if (_buildState != BuildState.Done) return;
        _buildState = BuildState.Idle;
        BuildPercent = 0;
        BuildMessage = "Flow changed since last build.";
        NotifyBuildState();
    }

    private void NotifyBuildState()
    {
        OnPropertyChanged(nameof(BuildLabel));
        OnPropertyChanged(nameof(IsBuilding));
    }

    private async Task BuildAsync()
    {
        if (_buildState == BuildState.Building) return;
        var n = Cells.Count;
        if (n == 0) { BuildMessage = "Nothing to build — the flow is empty."; return; }

        _buildState = BuildState.Building;
        BuildPercent = 8;
        BuildMessage = "Restoring packages…";
        NotifyBuildState();

        (int At, double Pct, string Msg)[] steps =
        [
            (400, 32, $"Emitting {n} test class{(n == 1 ? "" : "es")}…"),
            (800, 62, "csc → TestFlow.dll"),
            (1200, 88, "Signing and copying to bin\\Release\\net8.0\\"),
            (1600, 100, $"Build succeeded · TestFlow.dll · {n} tests · {24 + n * 6} KB"),
        ];

        var elapsed = 0;
        foreach (var (at, pct, msg) in steps)
        {
            await Task.Delay(at - elapsed);
            elapsed = at;
            BuildPercent = pct;
            BuildMessage = msg;
            if (pct >= 100) _buildState = BuildState.Done;
            NotifyBuildState();
        }
    }

    // ── assistant ───────────────────────────────────────────────────────────

    private void Send(string text)
    {
        var q = text.Trim();
        if (q.Length == 0) return;
        Chat.Add(new ChatMessageViewModel("You", q, true));
        Draft = "";
        _ = ReplyAsync(q);
    }

    private async Task ReplyAsync(string question)
    {
        AiThinking = true;
        var reply = await _assistant.AskAsync(question);
        AiThinking = false;

        var text = reply.Text;
        if (reply.Generated is { } generated)
        {
            AddCell(generated);
            text = text.Replace("{cell}", Cells.Count.ToString());
        }
        Chat.Add(new ChatMessageViewModel("Assistant", text, false));
    }
}
