using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace QModReloaded;

public class QMod
{
    public string Id { get; set; } = "mod_id";
    public string DisplayName { get; set; } = "Display name";
    public string Author { get; set; } = "author";
    public string Version { get; set; } = "0.0.0";
    public string[] Requires { get; set; } = Array.Empty<string>();
    public bool Enable { get; set; }
    public string AssemblyName { get; set; } = "dll filename";
    public string EntryMethod { get; set; } = "Namespace.Class.Method of Harmony.PatchAll or your equivalent";
    public int LoadOrder { get; set; } = -1;
    public string Priority { get; set; }

    [JsonIgnore] public Assembly LoadedAssembly { get; set; }

    [JsonIgnore]
    public string ModAssemblyPath { get; set; }

    public Dictionary<string, object> Config { get; set; }

    public static QMod FromJsonFile(string file)
	{
		try
		{
			var value = File.ReadAllText(file);
			return JsonConvert.DeserializeObject<QMod>(value);
		}
		catch (Exception ex)
		{
            Logger.WriteLog($"Parsing JSON failed: {ex.Message}");
			return null;
		}
	}
}
