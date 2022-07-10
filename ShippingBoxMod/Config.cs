using System;

namespace ShippingBoxMod;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        bool.TryParse(_con.Value("ShowSoldMessagesOnPlayer", "true"), out var showSoldMessagesOnPlayer);
        _options.ShowSoldMessagesOnPlayer = showSoldMessagesOnPlayer;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool ShowSoldMessagesOnPlayer = true;
    }
}