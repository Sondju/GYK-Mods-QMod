using System;
using System.Globalization;

namespace LargerScale;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        int.TryParse(_con.Value("GameScale", "2"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var gameScale);
        _options.GameScale = gameScale;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public int GameScale;
    }
}