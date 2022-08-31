using System;

namespace IBuildWhereIWant;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        bool.TryParse(_con.Value("DisableGrid", "true"), out var disableGrid);
        _options.DisableGrid = disableGrid;

        bool.TryParse(_con.Value("DisableGreyRemoveOverlay", "true"), out var disableGreyRemoveOverlay);
        _options.DisableGreyRemoveOverlay = disableGreyRemoveOverlay;
        
        bool.TryParse(_con.Value("DisableBuildingCollision", "false"), out var disableBuildingCollision);
        _options.DisableBuildingCollision = disableBuildingCollision;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool DisableGrid;
        public bool DisableGreyRemoveOverlay;
        public bool DisableBuildingCollision;
    }
}