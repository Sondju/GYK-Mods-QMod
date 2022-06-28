using System;

namespace BeamMeUpGerry
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool DisableCooldown;
            public bool HalfCooldown;
            public bool DisableGerryVoice;
            public bool DisableAlerts;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("DisableCooldown", "false"), out var disableCooldown);
            _options.DisableCooldown = disableCooldown;

            bool.TryParse(_con.Value("HalfCooldown", "true"), out var halfCooldown);
            _options.HalfCooldown = halfCooldown;

            bool.TryParse(_con.Value("DisableGerryVoice", "false"), out var disableGerryVoice);
            _options.DisableGerryVoice = disableGerryVoice;

            bool.TryParse(_con.Value("DisableAlerts", "false"), out var disableAlerts);
            _options.DisableAlerts = disableAlerts;

            _con.ConfigWrite();

            return _options;
        }
    }
}
