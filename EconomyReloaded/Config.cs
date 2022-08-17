using System;

namespace EconomyReloaded
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool Debug;
            public bool OldSchoolMode;
            public bool DisableInflation;
            public bool DisableDeflation;
        }

        //public static void WriteOptions()
        //{
        //    _con.UpdateValue("Debug", _options.Debug.ToString());
        //}

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("Debug", "false"), out var debug);
            _options.Debug = debug;

            bool.TryParse(_con.Value("OldSchoolMode", "false"), out var oldSchoolMode);
            _options.OldSchoolMode = oldSchoolMode;

            bool.TryParse(_con.Value("DisableInflation", "true"), out var disableInflation);
            _options.DisableInflation = disableInflation;

            bool.TryParse(_con.Value("DisableDeflation", "true"), out var disableDeflation);
            _options.DisableDeflation = disableDeflation;

            _con.ConfigWrite();

            return _options;
        }
    }
}

