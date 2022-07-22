using System;

namespace GerrysJunkTrunk;

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

        bool.TryParse(_con.Value("DisableSoldMessageWhenNoSale", "false"), out var disableSoldMessageWhenNoSale);
        _options.DisableSoldMessageWhenNoSale = disableSoldMessageWhenNoSale;

        bool.TryParse(_con.Value("EnableGerry", "true"), out var enableGerry);
        _options.EnableGerry = enableGerry;

        bool.TryParse(_con.Value("ShowSummary", "true"), out var showSummary);
        _options.ShowSummary = showSummary;

        bool.TryParse(_con.Value("ShowItemPriceTooltips", "true"), out var showItemPriceTooltips);
        _options.ShowItemPriceTooltips = showItemPriceTooltips;

        bool.TryParse(_con.Value("ShowKnownVendorCount", "true"), out var showKnownVendorCount);
        _options.ShowKnownVendorCount = showKnownVendorCount;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool ShowSoldMessagesOnPlayer = true;
        public bool DisableSoldMessageWhenNoSale;
        public bool EnableGerry;
        public bool ShowSummary;
        public bool ShowItemPriceTooltips;
        public bool ShowKnownVendorCount;
    }
}