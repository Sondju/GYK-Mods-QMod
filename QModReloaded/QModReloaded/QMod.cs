using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QModReloaded;

public class QMod
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Author { get; set; }
    public string Version { get; set; }
    public string[] Requires { get; set; } = Array.Empty<string>();
    public bool Enable { get; set; }
    public string AssemblyName { get; set; }
    public string EntryMethod { get; set; }
    public int LoadOrder { get; set; } = -1;
    public string Priority { get; set; }

    [JsonIgnore] 
    public Assembly LoadedAssembly { get; set; }

    [JsonIgnore] 
    public string ModAssemblyPath { get; set; }

    public Dictionary<string, object> Config { get; set; }

    public static QMod FromJsonFile(string file)
	{

        var value = File.ReadAllText(file);
        return JsonSerializer.Deserialize<QMod>(value);
    }
}
