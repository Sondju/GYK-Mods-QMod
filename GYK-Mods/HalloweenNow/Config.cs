using System;

namespace HalloweenNow
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool HalloweenNow;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("HalloweenNow", "true"), out var halloweenNow);
            _options.HalloweenNow = halloweenNow;

            _con.ConfigWrite();

            return _options;
        }
    }
}
