using System;
using System.Globalization;

namespace QueueEverything
{
    public static class FcConfig
    {
        private static Options _options;
        private static FcConfigReader _con;

        [Serializable]
        public class Options
        {
            public float CraftSpeedMultiplier;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new FcConfigReader();
            float.TryParse(_con.Value("CraftSpeedMultiplier", "2"), NumberStyles.Float, CultureInfo.InvariantCulture, out var craftSpeedMultiplier);
            _options.CraftSpeedMultiplier = craftSpeedMultiplier;

            _con.ConfigWrite();

            return _options;
        }
    }
}