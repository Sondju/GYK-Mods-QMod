using System;
using UnityEngine.Serialization;

namespace BeamMeUpGerry
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            [FormerlySerializedAs("FadeForCustomLocations")] public bool fadeForCustomLocations;
            [FormerlySerializedAs("IncreaseMenuAnimationSpeed")] public bool increaseMenuAnimationSpeed;
            [FormerlySerializedAs("EnableListExpansion")] public bool enableListExpansion;
            [FormerlySerializedAs("DisableGerry")] public bool disableGerry;
            [FormerlySerializedAs("Debug")] public bool debug;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("IncreaseMenuAnimationSpeed", "true"), out var increaseMenuAnimationSpeed);
            _options.increaseMenuAnimationSpeed = increaseMenuAnimationSpeed;

            bool.TryParse(_con.Value("FadeForCustomLocations", "true"), out var fadeForCustomLocations);
            _options.fadeForCustomLocations = fadeForCustomLocations;

            bool.TryParse(_con.Value("EnableListExpansion", "true"), out var enableListExpansion);
            _options.enableListExpansion = enableListExpansion;

            bool.TryParse(_con.Value("DisableGerry", "false"), out var disableGerry);
            _options.disableGerry = disableGerry;

            bool.TryParse(_con.Value("Debug", "false"), out var debug);
            _options.debug = debug;

            _con.ConfigWrite();

            return _options;
        }
    }
}