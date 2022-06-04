using System;

namespace QueueEverything
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool HalfFireRequirements;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("HalfFireRequirements", "true"), out var halfFireRequirements);
            _options.HalfFireRequirements = halfFireRequirements;

            _con.ConfigWrite();

            return _options;
        }
    }
}
