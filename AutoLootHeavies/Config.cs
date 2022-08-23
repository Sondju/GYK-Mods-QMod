using System;
using System.Globalization;
using UnityEngine;

namespace AutoLootHeavies;

public static class Config
{
    private static Options _options;
    private static ConfigReader _con;

    public static void WriteOptions()
    {
        _con.UpdateValue("TeleportWhenStockPilesFull", _options.TeleportWhenStockPilesFull.ToString());
        _con.UpdateValue("TeleportToDumpsite", _options.TeleportToDumpsite.ToString());
        _con.UpdateValue("DesignatedTimberLocation",
            $"{_options.DesignatedTimberLocation.x},{_options.DesignatedTimberLocation.y},{_options.DesignatedTimberLocation.z}".ToString(CultureInfo.InvariantCulture));
        _con.UpdateValue("DesignatedOreLocation",
            $"{_options.DesignatedOreLocation.x},{_options.DesignatedOreLocation.y},{_options.DesignatedOreLocation.z}".ToString(CultureInfo.InvariantCulture));
        _con.UpdateValue("DesignatedStoneLocation",
            $"{_options.DesignatedStoneLocation.x},{_options.DesignatedStoneLocation.y},{_options.DesignatedStoneLocation.z}".ToString(CultureInfo.InvariantCulture));
    }

    public static Options GetOptions()
    {
        _options = new Options();
        _con = new ConfigReader();

        bool.TryParse(_con.Value("TeleportWhenStockPilesFull", "true"), out var teleportWhenStockPilesFull);
        _options.TeleportWhenStockPilesFull = teleportWhenStockPilesFull;

        bool.TryParse(_con.Value("TeleportToDumpsite", "true"), out var teleportToDumpsite);
        _options.TeleportToDumpsite = teleportToDumpsite;

        bool.TryParse(_con.Value("TeleportToNearestStockpile", "true"), out var teleportToNearestStockpile);
        _options.TeleportToNearestStockpile = teleportToNearestStockpile;

        bool.TryParse(_con.Value("DisableImmersionMode", "false"), out var disableImmersionMode);
        _options.DisableImmersionMode = disableImmersionMode;

        float.TryParse(_con.Value("ScanIntervalInSeconds", "30"), out var scanIntervalInSeconds);
        _options.ScanIntervalInSeconds = scanIntervalInSeconds;

        var tempT = _con.Value("DesignatedTimberLocation", "-3712.003,6144,1294.643".ToString(CultureInfo.InvariantCulture)).Split(',');
        var tempO = _con.Value("DesignatedOreLocation", "-3712.003,6144,1294.643".ToString(CultureInfo.InvariantCulture)).Split(',');
        var tempS = _con.Value("DesignatedStoneLocation", "-3712.003,6144,1294.643".ToString(CultureInfo.InvariantCulture)).Split(',');

        _options.DesignatedTimberLocation =
            new Vector3(float.Parse(tempT[0], CultureInfo.InvariantCulture), float.Parse(tempT[1], CultureInfo.InvariantCulture), float.Parse(tempT[2], CultureInfo.InvariantCulture));
        _options.DesignatedOreLocation =
            new Vector3(float.Parse(tempO[0], CultureInfo.InvariantCulture), float.Parse(tempO[1], CultureInfo.InvariantCulture), float.Parse(tempO[2], CultureInfo.InvariantCulture));
        _options.DesignatedStoneLocation =
            new Vector3(float.Parse(tempS[0], CultureInfo.InvariantCulture), float.Parse(tempS[1], CultureInfo.InvariantCulture), float.Parse(tempS[2], CultureInfo.InvariantCulture));

        _con.ConfigWrite();

        return _options;
    }

    [Serializable]
    public class Options
    {
        public bool TeleportWhenStockPilesFull;
        public bool TeleportToDumpsite;
        public bool TeleportToNearestStockpile;
        public Vector3 DesignatedTimberLocation;
        public Vector3 DesignatedOreLocation;
        public Vector3 DesignatedStoneLocation;
        public float ScanIntervalInSeconds;
        public bool DisableImmersionMode;
    }
}