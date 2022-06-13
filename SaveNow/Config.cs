using System;

namespace SaveNow
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public int SaveInterval = 900000;
            public bool AutoSave = true;
            public bool NewFileOnAutoSave;
            public int AutoSavesToKeep = 5;
            public bool DisableAutoSaveInfo;
            public bool RemoveFromSaveListButKeepFile;
            public bool TurnOffTravelMessages;
            public bool TurnOffSaveGameNotificationText;
            public bool ExitToDesktop;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            int.TryParse(_con.Value("SaveInterval", "900000"), out var saveInterval);
            _options.SaveInterval = saveInterval;

            bool.TryParse(_con.Value("AutoSave", "true"), out var autoSave);
            _options.AutoSave = autoSave;

            bool.TryParse(_con.Value("NewFileOnAutoSave", "true"), out var newFileOnAutoSave);
            _options.NewFileOnAutoSave = newFileOnAutoSave;

            int.TryParse(_con.Value("AutoSavesToKeep", "5"), out var autoSavesToKeep);
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

            _con.ConfigWrite();

            return _options;
        }
    }
}