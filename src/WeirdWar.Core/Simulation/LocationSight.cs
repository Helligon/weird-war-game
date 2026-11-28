using System;
using System.Collections.Generic;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>
    /// Sight from owned locations (FOW-007). Read-only: it changes no state and nothing here is hashed (MUL-014, TEC-008).
    /// Reads P-FOW-03 by parameter ID at construction (DLV-014), so a missing or malformed value fails at game start:
    /// "hq=3 fob=2 major=2 command=1 supply=1".
    /// </summary>
    public sealed class LocationSight
    {
        private readonly Dictionary<LocationType, int> _radius = new Dictionary<LocationType, int>();

        public LocationSight(ParameterTable parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            var sight = parameters.GetKeyedInts("P-FOW-03", "hq", "fob", "major", "command", "supply");
            foreach (var kv in sight)
                if (kv.Value < 0) throw new ParameterFormatException("P-FOW-03: '" + kv.Key + "' must not be negative.");
            _radius[LocationType.Hq] = sight["hq"];
            _radius[LocationType.Fob] = sight["fob"];
            _radius[LocationType.Major] = sight["major"];
            _radius[LocationType.Command] = sight["command"];
            _radius[LocationType.Supply] = sight["supply"];
        }

        /// <summary>
        /// The sight radius of a location in hexes (FOW-007): the P-FOW-03 value for its current type (a Supply ST upgraded
        /// to an FOB uses the FOB value, MAP-012), or 0 when it is neutral, because only owned locations give sight.
        /// </summary>
        public int RadiusOf(LocationState location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            return location.Owner == null ? 0 : _radius[location.Type];
        }
    }
}
