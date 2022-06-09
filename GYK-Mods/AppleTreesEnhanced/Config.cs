using System;

namespace AppleTreesEnhanced
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool IncludeGardenBerryBushes;
            public bool IncludeGardenTrees;
            public bool IncludeWorldBerryBushes;
            public bool ShowHarvestReadyMessages;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("ShowHarvestReadyMessages", "true"), out var showHarvestReadyMessages);
            _options.ShowHarvestReadyMessages = showHarvestReadyMessages;

            bool.TryParse(_con.Value("IncludeGardenBerryBushes", "true"), out var includeGardenBerryBushes);
            _options.IncludeGardenBerryBushes = includeGardenBerryBushes;

            bool.TryParse(_con.Value("IncludeWorldBerryBushes", "true"), out var includeWorldBerryBushes);
            _options.IncludeWorldBerryBushes = includeWorldBerryBushes;

            bool.TryParse(_con.Value("IncludeGardenTrees", "true"), out var includeGardenTrees);
            _options.IncludeGardenTrees = includeGardenTrees;

            _con.ConfigWrite();

            return _options;
        }
    }
}
