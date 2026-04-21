using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Timesoft.Solution.Web3.Services
{
    internal static class AppSettings
    {
        public static string Read(string key)
        {
            string localValue = ReadFromLocalConfig(key);

            if (!string.IsNullOrWhiteSpace(localValue))
            {
                return localValue;
            }

            return ConfigurationManager.AppSettings[key];
        }

        private static string ReadFromLocalConfig(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            string localConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web.local.config");

            if (!File.Exists(localConfigPath))
            {
                return null;
            }

            try
            {
                XDocument document = XDocument.Load(localConfigPath);

                return document.Root?
                    .Element("appSettings")?
                    .Elements("add")
                    .Where(item => string.Equals((string)item.Attribute("key"), key, StringComparison.Ordinal))
                    .Select(item => (string)item.Attribute("value"))
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
