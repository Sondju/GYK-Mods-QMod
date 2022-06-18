using System;

namespace QueueEverything;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        bool.TryParse(_con.Value("HalfFireRequirements", "true"), out var halfFireRequirements);
        _options.HalfFireRequirements = halfFireRequirements;

        bool.TryParse(_con.Value("AutoMaxMultiQualCrafts", "true"), out var autoMaxMultiQualCrafts);
        _options.AutoMaxMultiQualCrafts = autoMaxMultiQualCrafts;

        bool.TryParse(_con.Value("AutoMaxNormalCrafts", "false"), out var autoMaxNormalCrafts);
        _options.AutoMaxNormalCrafts = autoMaxNormalCrafts;

        bool.TryParse(_con.Value("AutoSelectHighestQualRecipe", "true"), out var autoSelectHighestQualRecipe);
        _options.AutoSelectHighestQualRecipe = autoSelectHighestQualRecipe;

        bool.TryParse(_con.Value("AutoSelectCraftButtonWithController", "true"),
            out var autoSelectCraftButtonWithController);
        _options.AutoSelectCraftButtonWithController = autoSelectCraftButtonWithController;

        bool.TryParse(_con.Value("MakeEverythingAuto", "false"),
            out var makeEverythingAuto);
        _options.MakeEverythingAuto = makeEverythingAuto;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool HalfFireRequirements;
        public bool AutoMaxMultiQualCrafts;
        public bool AutoMaxNormalCrafts;
        public bool AutoSelectHighestQualRecipe;
        public bool AutoSelectCraftButtonWithController;
        public bool MakeEverythingAuto;
    }
}