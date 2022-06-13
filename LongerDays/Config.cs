using System;

namespace LongerDays
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool DoubleLengthDays;
            public bool EvenLongerDays;
            public bool Madness;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("DoubleLengthDays", "false"), out var doubleLengthDays);
            _options.DoubleLengthDays = doubleLengthDays;

            bool.TryParse(_con.Value("EvenLongerDays", "false"), out var evenLongerDays);
            _options.EvenLongerDays = evenLongerDays;

            bool.TryParse(_con.Value("Madness", "false"), out var madness);
            _options.Madness = madness;

            _con.ConfigWrite();

            return _options;
        }
    }
}