using System;

namespace LargerScale;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        var gameScaleBool = int.TryParse(_con.Value("GameScale", "2"), out var gameScale);
        _options.GameScale = gameScaleBool ? gameScale : 2;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public int GameScale = 2;
    }
}