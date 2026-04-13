using System.Configuration;

namespace Timesoft.Solution.Api.Web3.Services
{
    internal static class AppSettings
    {
        public static string Read(string primaryKey, string fallbackKey = null)
        {
            string value = ConfigurationManager.AppSettings[primaryKey];

            if (!string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(fallbackKey))
            {
                return value;
            }

            return ConfigurationManager.AppSettings[fallbackKey];
        }
    }
}
