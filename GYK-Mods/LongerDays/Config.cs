using System;

namespace LongerDays
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool DoubleLengthDays;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("DoubleLengthDays", "false"), out var doubleLengthDays);
            _options.DoubleLengthDays = doubleLengthDays;

            _con.ConfigWrite();

            return _options;
        }
    }
}
