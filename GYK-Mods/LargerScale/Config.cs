using System;

namespace LargerScale
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public int GameScale = 2;
            public int Width = 1920;
            public int Height = 1080;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            var gameScaleBool = int.TryParse(_con.Value("GameScale", "2"), out var gameScale);
            _options.GameScale = gameScaleBool ? gameScale : 2;

            var widthBool = int.TryParse(_con.Value("Width", "1920"), out var width);
            _options.Width = widthBool ? width : 1920;

            var heightBool = int.TryParse(_con.Value("Height", "1080"), out var height);
            _options.Height = heightBool ? height: 1080;

            _con.ConfigWrite();

            return _options;
        }
    }
}
