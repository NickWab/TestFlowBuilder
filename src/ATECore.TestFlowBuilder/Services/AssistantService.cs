using ATECore.TestFlowBuilder.Models;

namespace ATECore.TestFlowBuilder.Services;

/// <summary>A canned reply. <see cref="Generated"/> is a test the assistant wants appended; <c>{cell}</c> in the text becomes its cell number.</summary>
public sealed record AssistantReply(string Text, TestDefinition? Generated = null);

/// <summary>Stand-in for the AI assistant: keyword-matched replies, no model behind it yet.</summary>
public sealed class AssistantService
{
    private static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(750);

    public async Task<AssistantReply> AskAsync(string question)
    {
        await Task.Delay(Latency);
        var t = question.ToLowerInvariant();

        if (t.Contains("adc") || t.Contains("explain"))
        {
            return new AssistantReply(
                "The ADC cell sweeps an SMU across five setpoints and compares each converted code against the applied voltage.\n\n" +
                "It asserts Near(volts, target, 0.002), so a point fails if the converted value drifts more than 2 mV. " +
                "Last run: INL 0.8 LSB, offset 1.2 mV — comfortably inside the datasheet limits.");
        }

        if (t.Contains("1.8") || t.Contains("write") || t.Contains("create") || t.Contains("add"))
        {
            var basis = TestLibrary.Find("psu_rail")!;
            var generated = basis with
            {
                Name = "1.8V Rail Margin",
                Code = """
                    // 1.8V rail margin, CH2 (generated)
                    var psu = Hal.Open<IPowerSupply>("PSU-3021", channel: 2);

                    foreach (var v in new[] { 1.710, 1.800, 1.890 })  // -5%, nom, +5%
                    {
                        psu.SetVoltage(v);
                        psu.OutputEnable(true);
                        Delay.Ms(200);

                        var meas = psu.MeasureVoltage();
                        var dut  = Hal.Open<IDevice>("DUT");
                        Assert.Near(meas, v, 0.010, $"rail set {v:F3} V");
                        Assert.True(dut.SelfTest().Passed, $"DUT self-test @ {v:F3} V");
                    }
                    psu.OutputEnable(false);
                    """
            };
            return new AssistantReply(
                "Added \"1.8V Rail Margin\" as cell {cell}. It walks -5% / nominal / +5% on CH2 and runs the DUT self-test at each corner. " +
                "Edit the corners in the cell, then hit Run.",
                generated);
        }

        if (t.Contains("order"))
        {
            return new AssistantReply(
                "Put Versions first — if the firmware is wrong, every downstream limit is meaningless. " +
                "Power Supply next so the rails are up before the ADC sweep, and leave Thermal Soak last since it holds the bench for minutes.");
        }

        return new AssistantReply(
            "Noted. I can explain any cell, rewrite its C# against a different driver, or add a new test to the flow — tell me the signal and the limits and I will draft it.");
    }
}
