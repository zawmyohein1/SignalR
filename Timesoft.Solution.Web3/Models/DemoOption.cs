namespace Timesoft.Solution.Web3.Models
{
    public sealed class DemoOption
    {
        public DemoOption(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; }

        public string Text { get; }
    }
}
