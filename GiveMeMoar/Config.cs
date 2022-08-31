using System;
using UnityEngine.Serialization;

namespace GiveMeMoar
{
    public static class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            [FormerlySerializedAs("GratitudeMultiplier")] public int gratitudeMultiplier;
            [FormerlySerializedAs("SinShardMultiplier")] public int sinShardMultiplier;
            [FormerlySerializedAs("DonationMultiplier")] public int donationMultiplier;
            [FormerlySerializedAs("RedTechPointMultiplier")] public int redTechPointMultiplier;
            [FormerlySerializedAs("BlueTechPointMultiplier")] public int blueTechPointMultiplier;
            [FormerlySerializedAs("GreenTechPointMultiplier")] public int greenTechPointMultiplier;
            [FormerlySerializedAs("HappinessMultiplier")] public int happinessMultiplier;
            [FormerlySerializedAs("ResourceMultiplier")] public int resourceMultiplier;
            [FormerlySerializedAs("FaithMultiplier")] public int faithMultiplier;
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            int.TryParse(_con.Value("FaithMultiplier", "0"), out var faithMultiplier);
            _options.faithMultiplier = faithMultiplier;
            
            int.TryParse(_con.Value("ResourceMultiplier", "0"), out var resourceMultiplier);
            _options.resourceMultiplier = resourceMultiplier;
            
            int.TryParse(_con.Value("GratitudeMultiplier", "0"), out var gratitudeMultiplier);
            _options.gratitudeMultiplier = gratitudeMultiplier;

            int.TryParse(_con.Value("SinShardMultiplier", "0"), out var sinShardMultiplier);
            _options.sinShardMultiplier = sinShardMultiplier;
            
            int.TryParse(_con.Value("DonationMultiplier", "0"), out var donationMultiplier);
            _options.donationMultiplier = donationMultiplier;
            
            int.TryParse(_con.Value("BlueTechPointMultiplier", "0"), out var blueTechPointMultiplier);
            _options.blueTechPointMultiplier = blueTechPointMultiplier;
            
            int.TryParse(_con.Value("GreenTechPointMultiplier", "0"), out var greenTechPointMultiplier);
            _options.greenTechPointMultiplier = greenTechPointMultiplier;
            
            int.TryParse(_con.Value("RedTechPointMultiplier", "0"), out var redTechPointMultiplier);
            _options.redTechPointMultiplier = redTechPointMultiplier;
            
            int.TryParse(_con.Value("HappinessMultiplier", "0"), out var happinessMultiplier);
            _options.happinessMultiplier = happinessMultiplier;

            _con.ConfigWrite();

            return _options;
        }
    }
}