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
            
            _con.ConfigWrite();

            return _options;
        }
    }
}

