using System;
using UnityEngine.Serialization;

namespace AddStraightToTable
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            [FormerlySerializedAs("HideInvalidSelections")] public bool hideInvalidSelections;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("HideInvalidSelections", "true"), out var hideInvalidSelections);
            _options.hideInvalidSelections = hideInvalidSelections;

            return _options;
        }
    }
}