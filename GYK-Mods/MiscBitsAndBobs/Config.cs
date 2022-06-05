using System;

namespace MiscBitsAndBobs
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public int TavernInvIncrease;
            public int ToolStackSize;
            public bool ShowOnlyPersonalInventory;
            public bool DontShowEmptyRowsInInventory;
            public bool QuietMusicInGui;
            public bool AllowHandToolDestroy;
            public bool HalloweenNow;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            int.TryParse(_con.Value("TavernInvIncrease", "30"), out var tavernInvIncrease);
            _options.TavernInvIncrease = tavernInvIncrease;

            int.TryParse(_con.Value("ToolStackSize", "5"), out var toolStackSize);
            _options.ToolStackSize = toolStackSize;

            bool.TryParse(_con.Value("QuietMusicInGUI", "true"), out var quietMusicInGUI);
            _options.QuietMusicInGui = quietMusicInGUI;

            bool.TryParse(_con.Value("ShowOnlyPersonalInventory", "true"), out var showOnlyPersonalInventory);
            _options.ShowOnlyPersonalInventory = showOnlyPersonalInventory;

            bool.TryParse(_con.Value("DontShowEmptyRowsInInventory", "true"), out var dontShowEmptyRowsInInventory);
            _options.DontShowEmptyRowsInInventory = dontShowEmptyRowsInInventory;

            bool.TryParse(_con.Value("AllowHandToolDestroy", "true"), out var allowHandToolDestroy);
            _options.AllowHandToolDestroy = allowHandToolDestroy;

            bool.TryParse(_con.Value("HalloweenNow", "true"), out var halloweenNow);
            _options.HalloweenNow = halloweenNow;

            _con.ConfigWrite();

            return _options;
        }
    }
}
