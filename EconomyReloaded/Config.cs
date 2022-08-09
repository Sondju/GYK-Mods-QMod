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
            public bool DisableInflation = true;
            public bool DisableDeflation = true;
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

            bool.TryParse(_con.Value("DisableInflation", "true"), out var disableInflation);
            _options.DisableInflation = disableInflation;

            bool.TryParse(_con.Value("DisableDeflation", "true"), out var disableDeflation);
            _options.DisableDeflation = disableDeflation;

            _con.ConfigWrite();

            return _options;
        }
    }
}

