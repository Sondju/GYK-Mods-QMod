using System;
using System.Globalization;

namespace SaveNow;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        int.TryParse(_con.Value("SaveInterval", "600"),NumberStyles.Integer, CultureInfo.InvariantCulture, out var saveInterval);
        _options.SaveInterval = saveInterval;

        bool.TryParse(_con.Value("AutoSave", "true"), out var autoSave);
        _options.AutoSave = autoSave;

        bool.TryParse(_con.Value("NewFileOnAutoSave", "true"), out var newFileOnAutoSave);
        _options.NewFileOnAutoSave = newFileOnAutoSave;

        int.TryParse(_con.Value("AutoSavesToKeep", "5"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var autoSavesToKeep);
        _options.AutoSavesToKeep = autoSavesToKeep;

        bool.TryParse(_con.Value("DisableAutoSaveInfo", "false"), out var disableAutoSaveInfo);
        _options.DisableAutoSaveInfo = disableAutoSaveInfo;

        bool.TryParse(_con.Value("RemoveFromSaveListButKeepFile", "true"), out var removeFromSaveListButKeepFile);
        _options.RemoveFromSaveListButKeepFile = removeFromSaveListButKeepFile;

        bool.TryParse(_con.Value("TurnOffTravelMessages", "false"), out var turnOffTravelMessages);
        _options.TurnOffTravelMessages = turnOffTravelMessages;

        bool.TryParse(_con.Value("TurnOffSaveGameNotificationText", "false"),
            out var turnOffSaveGameNotificationText);
        _options.TurnOffSaveGameNotificationText = turnOffSaveGameNotificationText;

        bool.TryParse(_con.Value("ExitToDesktop", "false"), out var exitToDesktop);
        _options.ExitToDesktop = exitToDesktop;

        bool.TryParse(_con.Value("DisableSaveOnExit", "false"), out var disableSaveOnExit);
        _options.DisableSaveOnExit = disableSaveOnExit;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public int SaveInterval = 600;
        public bool AutoSave = true;
        public bool NewFileOnAutoSave;
        public int AutoSavesToKeep = 5;
        public bool DisableAutoSaveInfo;
        public bool RemoveFromSaveListButKeepFile;
        public bool TurnOffTravelMessages;
        public bool TurnOffSaveGameNotificationText;
        public bool ExitToDesktop;
        public bool DisableSaveOnExit;
    }
}