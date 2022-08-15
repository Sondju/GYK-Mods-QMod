using System.Globalization;

namespace Helper
{
    public static class CrossModFields
    {
        public static bool TalkingToNpc { get; set; }
        public static bool ConfigReloadShown { get; set; }
        public static float TimeOfDayFloat { get; internal set; }
        public static string Lang { get; internal set; }
        public static CultureInfo Culture { get; internal set; }
        public static bool CraftAnywhere { get; set; }
        public static WorldGameObject CurrentWgoInteraction { get; internal set; }
        public static WorldGameObject PreviousWgoInteraction { get; set; }
        public static bool IsVendor { get; internal set; }
        public static bool IsCraft { get; internal set; }
        public static bool IsChest { get; internal set; }
        public static bool IsBarman { get; internal set; }
        public static bool IsTavernCellar { get; internal set; }
        public static bool IsRefugee { get; internal set; }
        public static bool IsWritersTable { get; internal set; }
        public static bool IsInDungeon { get; internal set; }
    }
}
