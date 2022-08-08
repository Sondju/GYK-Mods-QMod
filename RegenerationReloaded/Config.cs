using System;
using System.Globalization;
using Steamworks;

namespace RegenerationReloaded
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool Debug;
            public bool ShowRegenUpdates = true;
            public float LifeRegen = 2f;
            public float EnergyRegen = 1f;
            public float RegenDelay = 5f;
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

            bool.TryParse(_con.Value("ShowRegenUpdates", "true"), out var showRegenUpdates);
            _options.ShowRegenUpdates = showRegenUpdates;

            float.TryParse(_con.Value("LifeRegen", "2"), out var lifeRegen);
            _options.LifeRegen = lifeRegen;

            float.TryParse(_con.Value("EnergyRegen", "1"), out var energyRegen);
            _options.EnergyRegen = energyRegen;

            float.TryParse(_con.Value("RegenDelay", "5"), out var regenDelay);
            _options.RegenDelay = regenDelay;

            _con.ConfigWrite();

            return _options;
        }
    }
}
