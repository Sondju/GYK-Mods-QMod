using System;

namespace WheresMaStorage
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool SharedCraftInventory;
            public bool DontShowEmptyRowsInInventory;
            public bool HideInvalidSelections;
            public bool ShowOnlyPersonalInventory;
            public int AdditionalInventorySpace = 50;
            public int StackSizeForStackables = 999;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("SharedCraftInventory", "true"), out var sharedCraftInventory);
            _options.SharedCraftInventory = sharedCraftInventory;

            bool.TryParse(_con.Value("DontShowEmptyRowsInInventory", "true"), out var dontShowEmptyRowsInInventory);
            _options.DontShowEmptyRowsInInventory = dontShowEmptyRowsInInventory;

            bool.TryParse(_con.Value("HideInvalidSelections", "true"), out var hideInvalidSelections);
            _options.HideInvalidSelections = hideInvalidSelections;

            bool.TryParse(_con.Value("ShowOnlyPersonalInventory", "true"), out var showOnlyPersonalInventory);
            _options.ShowOnlyPersonalInventory = showOnlyPersonalInventory;

            int.TryParse(_con.Value("AdditionalInventorySpace", "50"), out var additionalInventorySpace);
            _options.AdditionalInventorySpace = additionalInventorySpace;

            int.TryParse(_con.Value("StackSizeForStackables", "999"), out var stackSizeForStackables);
            _options.StackSizeForStackables = stackSizeForStackables;

            _con.ConfigWrite();

            return _options;
        }
    }
}
