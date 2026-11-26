using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using WeirdWar.Core.Commands;
using WeirdWar.Core.Maps;

namespace WeirdWar.Core.Simulation
{
    /// <summary>Outcome of applying a command: accepted, or rejected with a reason.</summary>
    public sealed class CommandResult
    {
        private CommandResult(bool accepted, string? reason) { Accepted = accepted; Reason = reason; }

        public bool Accepted { get; }
        public string? Reason { get; }

        public static CommandResult Accept() => new CommandResult(true, null);
        public static CommandResult Reject(string reason) => new CommandResult(false, reason);
    }

    /// <summary>Thrown by replay when a log does not reproduce: a rejected command or an out-of-order entry.</summary>
    public sealed class ReplayDivergenceException : Exception
    {
        public ReplayDivergenceException(string message) : base(message) { }
    }

    /// <summary>
    /// The whole game state (DLV-012, TEC-008). Deliberately minimal for now: the seed, the single seeded
    /// random source, the command log, a trivial tally for test commands, and (when started on a map) the map,
    /// Factions, locations with owners and Brigades. Later Stories add fields, and
    /// MUST add each one to <see cref="CanonicalText"/> so it is part of the hash.
    /// State changes only through <see cref="ApplyCommand"/> (DLV-013).
    /// </summary>
    public sealed class GameState
    {
        public GameState(ulong seed)
        {
            Seed = seed;
            Random = new SeededRandom(seed);
        }

        /// <summary>
        /// Starts a new game on a map (MAP-021): each Faction has only its HQ (owned) and its starting Brigade, every
        /// ST is neutral (MAP-015) and there are no FOBs. Throws <see cref="MapFormatException"/> when the map has no
        /// Factions or the setup names a Section type that is not in <paramref name="sectionTypes"/>.
        /// </summary>
        public GameState(ulong seed, HexMap map, SectionTypeTable sectionTypes) : this(seed)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (sectionTypes == null) throw new ArgumentNullException(nameof(sectionTypes));
            if (map.Factions.Count == 0) throw new MapFormatException("The map has no Factions, so a game cannot start on it");
            Map = map;
            SectionTypes = sectionTypes;
            var factions = new List<FactionState>();
            var brigades = new List<Brigade>();
            foreach (var setup in map.Setup)
            {
                factions.Add(new FactionState(setup.FactionId, setup.Reserve));
                var sections = new List<Section>();
                foreach (var typeId in setup.SectionTypeIds)
                {
                    if (!sectionTypes.TryGet(typeId, out var type))
                        throw new MapFormatException("Setup for \"" + setup.FactionId + "\" uses unknown Section type \"" + typeId + "\"");
                    sections.Add(new Section(typeId, type.Health));
                }
                brigades.Add(new Brigade(brigades.Count + 1, setup.FactionId, setup.BrigadeSp, sections));
            }
            var locations = new List<LocationState>();
            foreach (var l in map.Locations) locations.Add(new LocationState(l, l.Type == LocationType.Hq ? l.Faction : null));
            Factions = factions;
            Locations = locations;
            Brigades = brigades;
        }

        public ulong Seed { get; }

        /// <summary>The map this game is played on, or null for a state made without one (tests of the core only).</summary>
        public HexMap? Map { get; }

        /// <summary>The Section types in use, or null when there is no map.</summary>
        public SectionTypeTable? SectionTypes { get; }

        /// <summary>Factions sorted by id.</summary>
        public IReadOnlyList<FactionState> Factions { get; } = new List<FactionState>();

        /// <summary>Locations sorted by id.</summary>
        public IReadOnlyList<LocationState> Locations { get; } = new List<LocationState>();

        /// <summary>Brigades sorted by id. Ids start at 1.</summary>
        public IReadOnlyList<Brigade> Brigades { get; } = new List<Brigade>();

        /// <summary>
        /// The one random source. All random draws in the simulation MUST come from here (TEC-012).
        /// Internal, so only commands can draw (DLV-013).
        /// </summary>
        internal SeededRandom Random { get; }

        /// <summary>
        /// Running SHA-256 over the accepted commands: each entry folds into the previous digest, so
        /// hashing the state costs the same however long the log is (TEC-008). Starts as 32 zero bytes.
        /// </summary>
        internal byte[] LogDigest { get; private set; } = new byte[32];

        public CommandLog Log { get; } = new CommandLog();

        /// <summary>Trivial value changed by the test commands.</summary>
        public long Tally { get; internal set; }

        /// <summary>
        /// The single apply path. Validates first; a rejected command changes nothing and is not logged.
        /// An accepted command is applied and then appended to the log.
        /// </summary>
        public CommandResult ApplyCommand(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var reason = command.Validate(this);
            if (reason != null) return CommandResult.Reject(reason);
            var canonical = Encoding.UTF8.GetBytes(CommandJson.Canonical(command));
            command.Apply(this);
            Log.Append(command);
            LogDigest = Sha256(LogDigest, canonical);
            return CommandResult.Accept();
        }

        /// <summary>
        /// Rebuilds a state from a seed and a log. Throws <see cref="ReplayDivergenceException"/> if an entry
        /// is out of order (step is not its position) or a command is rejected.
        /// </summary>
        public static GameState Replay(ulong seed, CommandLog log, HexMap? map = null, SectionTypeTable? sectionTypes = null)
        {
            var state = map == null ? new GameState(seed) : new GameState(seed, map, sectionTypes ?? throw new ArgumentNullException(nameof(sectionTypes)));
            for (int i = 0; i < log.Entries.Count; i++)
            {
                var entry = log.Entries[i];
                if (entry.Step != i)
                    throw new ReplayDivergenceException("Log entry at position " + i + " has step " + entry.Step);
                var result = state.ApplyCommand(entry.Command);
                if (!result.Accepted)
                    throw new ReplayDivergenceException("Command at step " + i + " was rejected: " + result.Reason);
            }
            return state;
        }

        /// <summary>
        /// Canonical text of the state content: fixed field order, invariant formatting, and the chained log
        /// digest standing in for the log. No dictionary order, object identity or per-process hash codes.
        /// </summary>
        public string CanonicalText()
        {
            var sb = new StringBuilder();
            sb.Append("weirdwar.state.v1\n");
            sb.Append("seed=").Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("random=").Append(Random.Position.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("tally=").Append(Tally.ToString(CultureInfo.InvariantCulture)).Append('\n');
            // The map and Section types are fixed for a game, so they enter as one digest each (the text stays short).
            sb.Append("map=").Append(Map == null ? "none" : Map.Digest()).Append('\n');
            sb.Append("sectiontypes=").Append(SectionTypes == null ? "none" : Hex(Sha256(Encoding.UTF8.GetBytes(SectionTypes.CanonicalText())))).Append('\n');
            foreach (var f in Factions)
                sb.Append("faction=").Append(f.Id).Append(" reserve=").Append(f.Reserve.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var l in Locations)
                sb.Append("location=").Append(l.Id).Append(" owner=").Append(l.Owner ?? "-").Append('\n');
            foreach (var b in Brigades)
            {
                sb.Append("brigade=").Append(b.Id.ToString(CultureInfo.InvariantCulture)).Append(" faction=").Append(b.Faction)
                  .Append(" sp=").Append(b.SP.ToString(CultureInfo.InvariantCulture)).Append(" sections=");
                for (var i = 0; i < b.Sections.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(b.Sections[i].TypeId).Append(':').Append(b.Sections[i].Health.ToString(CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
            }
            sb.Append("log=").Append(Log.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("digest=").Append(Hex(LogDigest)).Append('\n');
            return sb.ToString();
        }

        /// <summary>SHA-256 of <see cref="CanonicalText"/> (UTF-8), as lowercase hex.</summary>
        public string ComputeHash()
        {
            return Hex(Sha256(Encoding.UTF8.GetBytes(CanonicalText())));
        }

        private static byte[] Sha256(byte[] first, byte[]? second = null)
        {
            using (var sha = SHA256.Create())
            {
                var data = new byte[first.Length + (second?.Length ?? 0)];
                Buffer.BlockCopy(first, 0, data, 0, first.Length);
                if (second != null) Buffer.BlockCopy(second, 0, data, first.Length, second.Length);
                return sha.ComputeHash(data);
            }
        }

        private static string Hex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
