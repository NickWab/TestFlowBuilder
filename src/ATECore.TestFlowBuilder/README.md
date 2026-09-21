# Test Flow Builder

WPF (.NET 8) implementation of `Design/Test Flow Builder.dc.html`, styled with the Modernist design system
(`Themes/Modernist.xaml` mirrors the tokens in the design's `styles.css`; Archivo is bundled under `Assets/Fonts`, OFL).

```
dotnet run --project src/ATECore.TestFlowBuilder
```

Optional flags (match the design's props): `--sample-flow`, `--auto-run`, `--project "Name"`.

Run results, the build and the AI assistant are simulated, as in the design: `Models/TestLibrary.cs` holds the canned
tests and results, `Services/AssistantService.cs` the keyword replies, and `MainViewModel.BuildAsync` the build steps.

## Logo and icon

The mark is a red tile with three cells stepping down and to the right (a test flow), the last step in ink.
`tools/make_icon.py` regenerates `Assets/Icons/app.ico` (exe, taskbar, window), `logo.png` and `logo.svg` with no
dependencies; `LogoImage` in `Themes/Modernist.xaml` is the same geometry, used for the large header logo.
