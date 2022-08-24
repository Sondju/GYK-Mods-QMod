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
            public bool SharedInventory;
            public bool DontShowEmptyRowsInInventory;
            public bool HideInvalidSelections;
            public bool ShowOnlyPersonalInventory;
            public int AdditionalInventorySpace = 20;
            public bool ModifyInventorySize;
            public bool ModifyStackSize;
            public int StackSizeForStackables = 999;
            public bool RemoveGapsBetweenSections;
            public bool RemoveGapsBetweenSectionsVendor;
            public bool HideStockpileWidgets;
            public bool HideTavernWidgets;
            public bool HideRefugeeWidgets;
            public bool HideWarehouseShopWidgets;
            public bool HideSoulWidgets;
            public bool DisableInventoryDimming;
            public bool ShowUsedSpaceInTitles;
            public bool ShowWorldZoneInTitles;
            public bool CacheEligibleInventories;
            public bool IncludeRefugeeDepot;
            public bool Debug;
            public bool EnableChiselStacking;
            public bool EnableToolAndPrayerStacking;
            public bool EnableGraveItemStacking;
            public bool EnablePenPaperInkStacking;
            public bool AllowHandToolDestroy;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("ModifyInventorySize", "true"), out var modifyInventorySize);
            _options.ModifyInventorySize = modifyInventorySize;

            bool.TryParse(_con.Value("EnableGraveItemStacking", "false"), out var enableGraveItemStacking);
            _options.EnableGraveItemStacking = enableGraveItemStacking;

            bool.TryParse(_con.Value("EnablePenPaperInkStacking", "false"), out var enablePenPaperInkStacking);
            _options.EnablePenPaperInkStacking = enablePenPaperInkStacking;

            bool.TryParse(_con.Value("EnableChiselStacking", "false"), out var enableChiselStacking);
            _options.EnableChiselStacking = enableChiselStacking;

            bool.TryParse(_con.Value("EnableToolAndPrayerStacking", "true"), out var enableToolAndPrayerStacking);
            _options.EnableToolAndPrayerStacking = enableToolAndPrayerStacking;

            bool.TryParse(_con.Value("AllowHandToolDestroy", "true"), out var allowHandToolDestroy);
            _options.AllowHandToolDestroy = allowHandToolDestroy;

            bool.TryParse(_con.Value("ModifyStackSize", "true"), out var modifyStackSize);
            _options.ModifyStackSize = modifyStackSize;

            bool.TryParse(_con.Value("IncludeRefugeeDepot", "false"), out var includeRefugeeDepot);
            _options.IncludeRefugeeDepot = includeRefugeeDepot;

            bool.TryParse(_con.Value("Debug", "false"), out var debug);
            _options.Debug = debug;

            bool.TryParse(_con.Value("SharedInventory", "true"), out var sharedInventory);
            _options.SharedInventory = sharedInventory;

            bool.TryParse(_con.Value("DontShowEmptyRowsInInventory", "true"), out var dontShowEmptyRowsInInventory);
            _options.DontShowEmptyRowsInInventory = dontShowEmptyRowsInInventory;

            bool.TryParse(_con.Value("CacheEligibleInventories", "false"), out var cacheEligibleInventories);
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

                 bool.TryParse(_con.Value("HideSoulWidgets", "true"), out var hideSoulWidgets);
            _options.HideSoulWidgets = hideSoulWidgets;

            bool.TryParse(_con.Value("HideWarehouseShopWidgets", "true"), out var hideWarehouseShopWidgets);
            _options.HideWarehouseShopWidgets = hideWarehouseShopWidgets;

            _con.ConfigWrite();

            return _options;
        }
    }
}