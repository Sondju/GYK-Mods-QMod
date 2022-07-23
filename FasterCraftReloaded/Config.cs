using System;
using System.Globalization;

namespace FasterCraftReloaded
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public float CraftSpeedMultiplier;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();
            float.TryParse(_con.Value("CraftSpeedMultiplier", "2"), NumberStyles.Float, CultureInfo.InvariantCulture, out var craftSpeedMultiplier);
            _options.CraftSpeedMultiplier = craftSpeedMultiplier;

            _con.ConfigWrite();

            return _options;
        }
    }
}