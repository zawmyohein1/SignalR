namespace Timesoft.Solution.Web3.Models
{
    public sealed class SelectOption
    {
        public SelectOption(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; }

        public string Text { get; }
    }
}
