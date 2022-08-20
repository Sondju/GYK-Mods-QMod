using System;

namespace TheSeedEqualizer;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        bool.TryParse(_con.Value("ModifyZombieGardens", "true"), out var modifyZombieGardens);
        _options.ModifyZombieGardens = modifyZombieGardens;

        bool.TryParse(_con.Value("ModifyZombieVineyards", "true"), out var modifyZombieVineyards);
        _options.ModifyZombieVineyards = modifyZombieVineyards;

        bool.TryParse(_con.Value("ModifyPlayerGardens", "false"), out var modifyPlayerGardens);
        _options.ModifyPlayerGardens = modifyPlayerGardens;

        bool.TryParse(_con.Value("ModifyRefugeeGardens", "true"), out var modifyRefugeeGardens);
        _options.ModifyRefugeeGardens = modifyRefugeeGardens;

        bool.TryParse(_con.Value("AddWasteToZombieGardens", "true"), out var addWasteToZombieGardens);
        _options.AddWasteToZombieGardens = addWasteToZombieGardens;

        bool.TryParse(_con.Value("AddWasteToZombieVineyards", "true"), out var addWasteToZombieVineyards);
        _options.AddWasteToZombieVineyards = addWasteToZombieVineyards;

        bool.TryParse(_con.Value("BoostPotentialSeedOutput", "true"), out var boostPotentialSeedOutput);
        _options.BoostPotentialSeedOutput = boostPotentialSeedOutput;

        bool.TryParse(_con.Value("Debug", "false"), out var debug);
        _options.Debug = debug;


        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool ModifyZombieGardens;
        public bool ModifyZombieVineyards;
        public bool ModifyPlayerGardens;
        public bool ModifyRefugeeGardens;
        public bool AddWasteToZombieGardens;
        public bool AddWasteToZombieVineyards;
        public bool BoostPotentialSeedOutput;
        public bool Debug;
    }
}