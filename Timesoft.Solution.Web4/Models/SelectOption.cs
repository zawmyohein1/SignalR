namespace Timesoft.Solution.Web4.Models;

public sealed class SelectOption(string value, string text)
{
    public string Value { get; } = value;

    public string Text { get; } = text;
}
