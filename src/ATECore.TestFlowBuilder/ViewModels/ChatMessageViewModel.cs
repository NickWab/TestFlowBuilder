namespace ATECore.TestFlowBuilder.ViewModels;

public sealed record ChatMessageViewModel(string Who, string Text, bool IsUser);

public sealed record SuggestionViewModel(string Key, string Label);
