using System.Collections.Generic;
using System.IO;

namespace AppleTreesEnhanced
{
    public class ConfigReader
    {
        private const string ConfigPath = "./QMods/AppleTreesEnhanced/config.ini";

        private readonly Dictionary<string, string> _values = new();

        public ConfigReader()
        {
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, "");
            }

            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) return;
                var splitString = line.Split('=');
                _values.Add(splitString[0].Trim(), splitString[1].Trim());
            }
        }

        public string Value(string name, string value = null)
        {
            if (_values != null && _values.ContainsKey(name))
            {
                return _values[name];
            }

            _values?.Add(name.Trim(), value?.Trim());
            return value;
        }

        public void ConfigWrite()
        {
            using var file = new StreamWriter(ConfigPath, false);
            foreach (var entry in _values)
                file.WriteLine("{0}={1}", entry.Key.Trim(), entry.Value.Trim());
        }
    }
}