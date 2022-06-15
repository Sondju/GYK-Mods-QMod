using System;

namespace ThoughtfulReminders;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        bool.TryParse(_con.Value("SpeechBubbles", "true"), out var speechBubbles);
        _options.SpeechBubbles = speechBubbles;

        bool.TryParse(_con.Value("DaysOnly", "false"), out var daysOnly);
        _options.DaysOnly = daysOnly;

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool SpeechBubbles;
        public bool DaysOnly;
    }
}