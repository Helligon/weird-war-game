using System;
using System.Collections.Generic;
using WeirdWar.Core.Serialization;

namespace WeirdWar.Core.Commands
{
    /// <summary>The one canonical JSON form of a command, used for saves, the network and the state hash (TEC-008).</summary>
    internal static class CommandJson
    {
        public static Dictionary<string, object?> ToValue(ICommand command)
        {
            var d = command.ToJson();
            if (d.ContainsKey("type")) throw new InvalidOperationException("'type' is reserved by the serialiser");
            d["type"] = command.Type;
            return d;
        }

        public static string Canonical(ICommand command) => Json.Write(ToValue(command));
    }
}
