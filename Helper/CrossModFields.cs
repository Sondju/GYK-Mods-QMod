using System.Globalization;

namespace Helper
{
    public static class CrossModFields
    {
        public static bool TalkingToNPC { get; set; }
        public static bool ConfigReloadShown { get; set; }
        public static float TimeOfDayFloat { get; set; }
        public static string Lang { get; set; }
        public static CultureInfo Culture { get; set; }
        public static bool CraftAnywhere { get; set; }
        public static WorldGameObject CurrentWgoInteraction { get; set; }
        public static WorldGameObject PreviousWgoInteraction { get; set; }
        public static bool IsVendor { get; set; }
        public static bool IsCraft { get; set; }
        public static bool IsChest { get; set; }
        public static bool IsBarman { get; set; }
        public static bool IsTavernCellar { get; set; }
        public static bool IsRefugee { get; set; }
        public static bool IsWritersTable { get; set; }
    }
}
