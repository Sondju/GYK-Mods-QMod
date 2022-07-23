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
            public bool FadeForCustomLocations;
            public bool IncreaseMenuAnimationSpeed;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();
            
            bool.TryParse(_con.Value("IncreaseMenuAnimationSpeed", "true"), out var increaseMenuAnimationSpeed);
            _options.IncreaseMenuAnimationSpeed = increaseMenuAnimationSpeed;

            bool.TryParse(_con.Value("FadeForCustomLocations", "true"), out var fadeForCustomLocations);
            _options.FadeForCustomLocations = fadeForCustomLocations;

            _con.ConfigWrite();

            return _options;
        }
    }
}
