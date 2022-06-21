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

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool DisableGrid;
    }
}