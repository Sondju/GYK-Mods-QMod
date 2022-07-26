using System;
using System.Globalization;

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
            public int AdditionalInventorySpace = 20;
            public int StackSizeForStackables = 999;
            public bool RemoveGapsBetweenSections;
            public bool RemoveGapsBetweenSectionsVendor;
            public bool HideStockpileWidgets;
            public bool HideTavernWidgets;
            public bool HideRefugeeWidgets;
            public bool HideWarehouseShopWidgets;
            public bool DisableInventoryDimming;
            public bool ShowUsedSpaceInTitles;
            public bool ShowWorldZoneInTitles;
            public bool CacheEligibleInventories;
            public bool IncludeRefugeeDepot;
            public bool Debug;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("IncludeRefugeeDepot", "false"), out var includeRefugeeDepot);
            _options.IncludeRefugeeDepot = includeRefugeeDepot;

            bool.TryParse(_con.Value("Debug", "false"), out var debug);
            _options.Debug = debug;

            bool.TryParse(_con.Value("SharedCraftInventory", "true"), out var sharedCraftInventory);
            _options.SharedCraftInventory = sharedCraftInventory;

            bool.TryParse(_con.Value("DontShowEmptyRowsInInventory", "true"), out var dontShowEmptyRowsInInventory);
            _options.DontShowEmptyRowsInInventory = dontShowEmptyRowsInInventory;

            bool.TryParse(_con.Value("CacheEligibleInventories", "true"), out var cacheEligibleInventories);
            _options.CacheEligibleInventories = cacheEligibleInventories;

            bool.TryParse(_con.Value("ShowUsedSpaceInTitles", "true"), out var showUsedSpaceInTitles);
            _options.ShowUsedSpaceInTitles = showUsedSpaceInTitles;

            bool.TryParse(_con.Value("DisableInventoryDimming", "true"), out var disableInventoryDimming);
            _options.DisableInventoryDimming = disableInventoryDimming;

            bool.TryParse(_con.Value("ShowWorldZoneInTitles", "true"), out var showWorldZoneInTitles);
            _options.ShowWorldZoneInTitles = showWorldZoneInTitles;

            bool.TryParse(_con.Value("HideInvalidSelections", "true"), out var hideInvalidSelections);
            _options.HideInvalidSelections = hideInvalidSelections;

            bool.TryParse(_con.Value("RemoveGapsBetweenSections", "true"), out var removeGapsBetweenSections);
            _options.RemoveGapsBetweenSections = removeGapsBetweenSections;

            bool.TryParse(_con.Value("RemoveGapsBetweenSectionsVendor", "true"), out var removeGapsBetweenSectionsVendor);
            _options.RemoveGapsBetweenSectionsVendor = removeGapsBetweenSectionsVendor;

            bool.TryParse(_con.Value("ShowOnlyPersonalInventory", "true"), out var showOnlyPersonalInventory);
            _options.ShowOnlyPersonalInventory = showOnlyPersonalInventory;

            int.TryParse(_con.Value("AdditionalInventorySpace", "20"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var additionalInventorySpace);
            _options.AdditionalInventorySpace = additionalInventorySpace;

            int.TryParse(_con.Value("StackSizeForStackables", "999"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stackSizeForStackables);
            if (stackSizeForStackables > 999)
            {
                stackSizeForStackables = 999;
            }
            _options.StackSizeForStackables = stackSizeForStackables;

            bool.TryParse(_con.Value("HideStockpileWidgets", "true"), out var hideStockpileWidgets);
            _options.HideStockpileWidgets = hideStockpileWidgets;

            bool.TryParse(_con.Value("HideTavernWidgets", "true"), out var hideTavernWidgets);
            _options.HideTavernWidgets = hideTavernWidgets;

            bool.TryParse(_con.Value("HideRefugeeWidgets", "true"), out var hideRefugeeWidgets);
            _options.HideRefugeeWidgets = hideRefugeeWidgets;

            bool.TryParse(_con.Value("HideWarehouseShopWidgets", "true"), out var hideWarehouseShopWidgets);
            _options.HideWarehouseShopWidgets = hideWarehouseShopWidgets;
            
            _con.ConfigWrite();

            return _options;
        }
    }
}
