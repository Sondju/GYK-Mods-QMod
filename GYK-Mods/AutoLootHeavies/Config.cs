using System;
using UnityEngine;

namespace AutoLootHeavies
{
    public class Config
    {
        private static Options _options;
        private static ConfigReader _con;

        [Serializable]
        public class Options
        {
            public bool Teleportation;
            public bool DistanceBasedTeleport;
            public Vector3 DesignatedTimberLocation;
            public Vector3 DesignatedOreLocation;
            public Vector3 DesignatedStoneLocation;
            public float ScanIntervalInSeconds;
            public bool DisableImmersionMode;

        }

        public static void WriteOptions()
        {
            _con.UpdateValue("TeleportWhenStockPilesFull", _options.Teleportation.ToString());
            _con.UpdateValue("DistanceBasedTeleport", _options.DistanceBasedTeleport.ToString());
            _con.UpdateValue("DesignatedTimberLocation", $"{_options.DesignatedTimberLocation.x},{_options.DesignatedTimberLocation.y},{_options.DesignatedTimberLocation.z}");
            _con.UpdateValue("DesignatedOreLocation", $"{_options.DesignatedOreLocation.x},{_options.DesignatedOreLocation.y},{_options.DesignatedOreLocation.z}");
            _con.UpdateValue("DesignatedStoneLocation", $"{_options.DesignatedStoneLocation.x},{_options.DesignatedStoneLocation.y},{_options.DesignatedStoneLocation.z}");
        }

        public static Options GetOptions()
        {
            _options = new Options();
            _con = new ConfigReader();

            bool.TryParse(_con.Value("TeleportWhenStockPilesFull", "true"), out var teleportWhenStockPilesFull);
            _options.Teleportation = teleportWhenStockPilesFull;

            bool.TryParse(_con.Value("DistanceBasedTeleport", "true"), out var distanceBasedTeleport);
            _options.DistanceBasedTeleport = distanceBasedTeleport;

            bool.TryParse(_con.Value("DisableImmersionMode", "false"), out var disableImmersionMode);
            _options.DisableImmersionMode = disableImmersionMode;

            float.TryParse(_con.Value("ScanIntervalInSeconds", "30"), out var scanIntervalInSeconds);
            _options.ScanIntervalInSeconds = scanIntervalInSeconds;

            var tempT = _con.Value("DesignatedTimberLocation", "-3712.003,6144,1294.643").Split(',');
            var tempO = _con.Value("DesignatedOreLocation", "-3712.003,6144,1294.643").Split(',');
            var tempS = _con.Value("DesignatedStoneLocation", "-3712.003,6144,1294.643").Split(',');

            _options.DesignatedTimberLocation = new Vector3(float.Parse(tempT[0]), float.Parse(tempT[1]), float.Parse(tempT[2]));
            _options.DesignatedOreLocation = new Vector3(float.Parse(tempO[0]), float.Parse(tempO[1]), float.Parse(tempO[2]));
            _options.DesignatedStoneLocation = new Vector3(float.Parse(tempS[0]), float.Parse(tempS[1]), float.Parse(tempS[2]));

            _con.ConfigWrite();

            return _options;
        }
    }
}
