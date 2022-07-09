using System;

namespace ShippingBoxMod;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;


    public static void WriteOptions()
    {
        _con.UpdateValue("ShippingBoxBuilt", _options.ShippingBoxBuilt.ToString());
    }

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        bool.TryParse(_con.Value("ShippingBoxBuilt", "false"), out var shippingBoxBuilt);
        _options.ShippingBoxBuilt = shippingBoxBuilt;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool ShippingBoxBuilt;
    }
}