using System;
using System.IO;
using System.Text;

namespace UltraWide
{
    public class Config
    {
        private static Options _options;

        public class Options
        {
            public int Width = 1920;
            public int Height = 1080;
        }

        public static Options GetOptions()
        {
            if (_options != null) return _options;
            _options = new Options();
            const string path = "./QMods/UltraWide/config.txt";
            if (File.Exists(path))
            {
                var config = File.ReadAllLines(path);
                if (config.Length > 0)
                {
                    _options.Width = Convert.ToInt32(config[0]);
                    _options.Height = Convert.ToInt32(config[1]);
                }
                else
                {
                    var defaultOptions = new[] { _options.Width.ToString(), _options.Height.ToString() };
                    File.WriteAllLines(path, defaultOptions, Encoding.Default);
                    return _options;
                }
            }
            else
            {
                var defaultOptions = new[] {_options.Width.ToString(), _options.Height.ToString() };
                File.WriteAllLines(path, defaultOptions, Encoding.Default);
                return _options;
            }
            return _options;
        }
    }
}
