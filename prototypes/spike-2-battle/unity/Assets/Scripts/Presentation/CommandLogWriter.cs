using System;
using System.Globalization;
using System.IO;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// TEC-005, PCH-009: writes the finished battle's command log to disk. The core makes the JSON text (step-based times only);
    /// this Unity-side class adds the file name, which carries a wall-clock stamp so logs never overwrite each other.
    /// The wall clock never reaches the core (TEC-008).
    /// </summary>
    public static class CommandLogWriter
    {
        /// <summary>Writes the log into <paramref name="folder"/> (made if missing) and returns the full path of the file.</summary>
        public static string Write(BattleState battle, string folder, DateTime now)
        {
            Directory.CreateDirectory(folder);
            var name = "command-log-" + now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + battle.Outcome + ".json";
            var path = Path.GetFullPath(Path.Combine(folder, name));
            File.WriteAllText(path, CommandLogFile.Write(battle));
            return path;
        }
    }
}
