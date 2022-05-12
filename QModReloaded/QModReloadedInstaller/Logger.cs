using System.IO;

namespace QModReloaded;

public static class Logger
{
   private const string Log = "qmod_reloaded_log.txt";

    public static void WriteLog(string msg)
    {
        using var streamWriter = new StreamWriter(Log, append: true);
        streamWriter.WriteLine(msg);
    }
}
