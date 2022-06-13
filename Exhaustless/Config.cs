using System;

namespace Exhaustless
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool SpeedUpSleep;
            public bool SpeedUpMeditation;
            public bool YawnMessage;
            public bool SpendHalfEnergy;
            public bool SpendHalfSanity;
            public bool AutoWakeFromMeditation;
            public bool MakeToolsLastLonger;
            public bool AutoEquipNewTool;
            public bool SpendHalfGratitude;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("MakeToolsLastLonger", "true"), out var makeToolsLastLonger);
            _options.MakeToolsLastLonger = makeToolsLastLonger;

            bool.TryParse(_con.Value("SpendHalfGratitude", "true"), out var spendHalfGratitude);
            _options.SpendHalfGratitude = spendHalfGratitude;

            bool.TryParse(_con.Value("AutoEquipNewTool", "true"), out var autoEquipNewTool);
            _options.AutoEquipNewTool = autoEquipNewTool;

            bool.TryParse(_con.Value("SpeedUpSleep", "true"), out var speedUpSleep);
            _options.SpeedUpSleep = speedUpSleep;

            bool.TryParse(_con.Value("AutoWakeFromMeditation", "true"), out var autoWakeFromMeditation);
            _options.AutoWakeFromMeditation = autoWakeFromMeditation;

            bool.TryParse(_con.Value("SpendHalfSanity", "true"), out var spendHalfSanity);
            _options.SpendHalfSanity = spendHalfSanity;

            bool.TryParse(_con.Value("SpeedUpMeditation", "true"), out var speedUpMeditation);
            _options.SpeedUpMeditation = speedUpMeditation;

            bool.TryParse(_con.Value("YawnMessage", "true"), out var yawnMessage);
            _options.YawnMessage = yawnMessage;

            bool.TryParse(_con.Value("SpendHalfEnergy", "true"), out var spendHalfEnergy);
            _options.SpendHalfEnergy = spendHalfEnergy;

            _con.ConfigWrite();

            return _options;
        }
    }
}