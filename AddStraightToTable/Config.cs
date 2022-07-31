using System;

namespace AddStraightToTable
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool HideInvalidSelections;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("HideInvalidSelections", "true"), out var hideInvalidSelections);
            _options.HideInvalidSelections = hideInvalidSelections;

            return _options;
        }
    }
}