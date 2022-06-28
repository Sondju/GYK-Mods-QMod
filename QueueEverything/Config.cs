using System;

namespace QueueEverything;

public static class Config
{
    private static ConfigReader _con;
    private static Options _options;
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

        bool.TryParse(_con.Value("MakeEverythingAuto", "true"),
            out var makeEverythingAuto);
        _options.MakeEverythingAuto = makeEverythingAuto;

        bool.TryParse(_con.Value("MakeHandTasksAuto", "false"),
            out var makeHandTasksAuto);
        _options.MakeHandTasksAuto = makeHandTasksAuto;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool AutoMaxMultiQualCrafts;
        public bool AutoMaxNormalCrafts;
        public bool AutoSelectCraftButtonWithController;
        public bool AutoSelectHighestQualRecipe;
        public bool HalfFireRequirements;
        public bool MakeEverythingAuto;
        public bool MakeHandTasksAuto;
    }
}