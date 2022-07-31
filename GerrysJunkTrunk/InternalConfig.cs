using System;

namespace GerrysJunkTrunk;

public static class InternalConfig
{
    private static Options _options;
    private static InternalConfigReader _con;

    public static void WriteOptions()
    {
        _con.UpdateValue("ShippingBoxBuilt", _options.ShippingBoxBuilt.ToString());
        _con.UpdateValue("ShowIntroMessage", _options.ShowIntroMessage.ToString());
    }

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new InternalConfigReader();

        bool.TryParse(_con.Value("ShippingBoxBuilt", "false"), out var shippingBoxBuilt);
        _options.ShippingBoxBuilt = shippingBoxBuilt;

        bool.TryParse(_con.Value("ShowIntroMessage", "true"), out var showIntroMessage);
        _options.ShowIntroMessage = showIntroMessage;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool ShippingBoxBuilt;
        public bool ShowIntroMessage;
    }
}