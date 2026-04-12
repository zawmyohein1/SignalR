namespace JobRealtimeSample.MvcUi.Models;

public sealed class DemoOption(string value, string text)
{
    public string Value { get; } = value;

    public string Text { get; } = text;
}

