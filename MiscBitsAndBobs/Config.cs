using System;

namespace MiscBitsAndBobs;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();
        
        bool.TryParse(_con.Value("QuietMusicInGUI", "true"), out var quietMusicInGui);
        _options.QuietMusicInGui = quietMusicInGui;

        bool.TryParse(_con.Value("CondenseXpBar", "true"), out var condenseXpBar);
        _options.CondenseXpBar = condenseXpBar;

        bool.TryParse(_con.Value("ModifyPlayerMovementSpeed", "true"), out var modifyPlayerMovementSpeed);
        _options.ModifyPlayerMovementSpeed = modifyPlayerMovementSpeed;

        var playerMs = float.TryParse(_con.Value("PlayerMovementSpeed", "1.0"), out var playerMovementSpeed);
        if (playerMs)
        {
            _options.PlayerMovementSpeed = playerMovementSpeed < 1 ? 1.0f : playerMovementSpeed;
        }

        bool.TryParse(_con.Value("ModifyPorterMovementSpeed", "true"), out var modifyPorterMovementSpeed);
        _options.ModifyPorterMovementSpeed = modifyPorterMovementSpeed;

        var porterMs = float.TryParse(_con.Value("PorterMovementSpeed", "1.0"), out var porterMovementSpeed);
        if (porterMs)
        {
            _options.PorterMovementSpeed = porterMovementSpeed < 1 ? 1.0f : porterMovementSpeed;
        }

        bool.TryParse(_con.Value("HalloweenNow", "false"), out var halloweenNow);
        _options.HalloweenNow = halloweenNow;

        bool.TryParse(_con.Value("HideCreditsButtonOnMainMenu", "true"), out var hideCreditsButtonOnMainMenu);
        _options.HideCreditsButtonOnMainMenu = hideCreditsButtonOnMainMenu;

        bool.TryParse(_con.Value("SkipIntroVideoOnNewGame", "false"), out var skipIntroVideoOnNewGame);
        _options.SkipIntroVideoOnNewGame = skipIntroVideoOnNewGame;

        bool.TryParse(_con.Value("DisableCinematicLetterboxing", "true"), out var disableCinematicLetterboxing);
        _options.DisableCinematicLetterboxing = disableCinematicLetterboxing;

        bool.TryParse(_con.Value("KitsuneKitoMode", "false"), out var kitsuneKitoMode);
        _options.KitsuneKitoMode = kitsuneKitoMode;

        bool.TryParse(_con.Value("LessenFootprintImpact", "false"), out var lessenFootprintImpact);
        _options.LessenFootprintImpact = lessenFootprintImpact;

        bool.TryParse(_con.Value("RemovePrayerOnUse", "false"), out var removePrayerOnUse);
        _options.RemovePrayerOnUse = removePrayerOnUse;

        bool.TryParse(_con.Value("AddCoalToTavernOven", "true"), out var addCoalToTavernOven);
        _options.AddCoalToTavernOven = addCoalToTavernOven;

        bool.TryParse(_con.Value("AddZombiesToPyreAndCrematorium", "true"), out var addZombiesToPyreAndCrematorium);
        _options.AddZombiesToPyreAndCrematorium = addZombiesToPyreAndCrematorium;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool ModifyPlayerMovementSpeed;
        public float PlayerMovementSpeed = 1.0f;
        public bool ModifyPorterMovementSpeed;
        public float PorterMovementSpeed = 1.0f;
        public bool QuietMusicInGui;
        public bool HalloweenNow;
        public bool HideCreditsButtonOnMainMenu;
        public bool CondenseXpBar;
        public bool SkipIntroVideoOnNewGame;
        public bool DisableCinematicLetterboxing;
        public bool KitsuneKitoMode;
        public bool LessenFootprintImpact;
        public bool RemovePrayerOnUse;
        public bool AddCoalToTavernOven;
        public bool AddZombiesToPyreAndCrematorium;
    }
}