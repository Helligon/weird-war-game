using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        /// ST is neutral (MAP-015) and there are no FOBs. With <paramref name="cards"/> each Faction gets its deck and starting
        /// hand (CRD-003, CRD-004, CRD-016, CRD-018); without it the state has no Cards. Throws <see cref="MapFormatException"/> when the map has no
        /// Factions or the setup names a Section type that is not in <paramref name="sectionTypes"/>.
        /// </summary>
        public GameState(ulong seed, HexMap map, SectionTypeTable sectionTypes, ParameterTable parameters, CardTable? cards = null)
            : this(seed, map, sectionTypes, parameters, null, cards)
        {
        }

        /// <summary>As the public constructor, with a phase observer attached before the first turn begins (tests only).</summary>
        internal GameState(ulong seed, HexMap map, SectionTypeTable sectionTypes, ParameterTable parameters, Action<TurnPhase, string>? phaseObserver, CardTable? cards = null) : this(seed)
        {
            PhaseObserver = phaseObserver;
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (sectionTypes == null) throw new ArgumentNullException(nameof(sectionTypes));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            var startingSize = parameters.GetInt("P-BRG-05");
            // RES-013, BRG-036, DLV-014: starting values come from the data, never from the map.
            var startingReserve = StartingValue(parameters, "P-RES-05");
            var startingCp = StartingValue(parameters, "P-RES-06");
            var startingBrigadeSp = StartingValue(parameters, "P-BRG-09");
            if (map.Factions.Count == 0) throw new MapFormatException("The map has no Factions, so a game cannot start on it");
            IncomeRules = new IncomeRules(parameters);
            Map = map;
            SectionTypes = sectionTypes;
            Supply = new Supply(sectionTypes, parameters);
            MovementRules = new MovementRules(parameters);
            Territory = new TerritoryRules(parameters);
            BrigadeRules = new BrigadeRules(parameters);
            FobRules = new FobRules(parameters);
            _parametersDigest = parameters.Digest();
            var factions = new List<FactionState>();
            var brigades = new List<Brigade>();
            foreach (var setup in map.Setup)
            {
                factions.Add(new FactionState(setup.FactionId, startingReserve, startingCp));
                if (setup.SectionTypeIds.Count != startingSize)
                    throw new MapFormatException("Setup for \"" + setup.FactionId + "\" has " + setup.SectionTypeIds.Count
                        + " Sections; the starting Brigade has " + startingSize + " (P-BRG-05)");
                var sections = new List<Section>();
                foreach (var typeId in setup.SectionTypeIds)
                {
                    if (!sectionTypes.TryGet(typeId, out var type))
                        throw new MapFormatException("Setup for \"" + setup.FactionId + "\" uses unknown Section type \"" + typeId + "\"");
                    sections.Add(new Section(typeId, type.Health));
                }
                var hq = map.Locations.Single(l => l.Type == LocationType.Hq && l.Faction == setup.FactionId);
                brigades.Add(new Brigade(brigades.Count + 1, setup.FactionId, startingBrigadeSp, hq.Position, sections));
            }
            var locations = new List<LocationState>();
            foreach (var l in map.Locations) locations.Add(new LocationState(l, l.Type == LocationType.Hq ? l.Faction : null));
            Factions = factions;
            Locations = locations;
            _brigades = brigades;
            _nextBrigadeId = brigades.Count + 1;
            // TRN-033: the first player comes from the state's seeded source, so the same seed gives the same one.
            // Drawn here, as part of making the new game; replay rebuilds the state the same way.
            ActiveFaction = factions[Random.NextInt(factions.Count)].Id;
            if (cards != null) DealStartingHands(cards, parameters, factions);
            TurnNumber = 1;
            TurnPhases.BeginTurn(this, ActiveFaction);
        }

        /// <summary>
        /// CRD-016: each Faction's deck holds every listed Card, with its listed copies, shuffled from the seeded source in
        /// Faction order (TEC-008). CRD-004, P-CRD-01: the top Cards go to the hand. TRN-033, CRD-018, P-TRN-01: the Faction that
        /// goes second takes extra. A deck smaller than the hand leaves a smaller hand (CD1 has no Cards in the shipped data).
        /// </summary>
        private void DealStartingHands(CardTable cards, ParameterTable parameters, List<FactionState> factions)
        {
            var handSize = StartingValue(parameters, "P-CRD-01");
            var extra = StartingValue(parameters, "P-TRN-01");
            _cards = cards;
            _cardsPerTurn = StartingValue(parameters, "P-CRD-02");
            var second = NextFaction(ActiveFaction!);
            foreach (var f in factions)
            {
                foreach (var card in cards.All)
                    for (var i = 0; i < card.Copies; i++) f._deck.Add(card.Id);
                for (var i = f._deck.Count - 1; i > 0; i--)
                {
                    var j = Random.NextInt(i + 1);
                    var t = f._deck[i]; f._deck[i] = f._deck[j]; f._deck[j] = t;
                }
                var n = Math.Min(f._deck.Count, handSize + (f.Id == second && second != ActiveFaction ? extra : 0));
                f._hand.AddRange(f._deck.GetRange(0, n));
                f._deck.RemoveRange(0, n);
            }
        }

        /// <summary>The Card data, or null for a game without Cards. Fixed for a game; enters the hash as a digest.</summary>
        internal CardTable? Cards => _cards;
        private CardTable? _cards;

        /// <summary>Cards each Faction draws in its Draw step (P-CRD-02). Fixed for a game; its parameter is in the digest.</summary>
        internal int CardsPerTurn => _cardsPerTurn;
        private int _cardsPerTurn;

        private static int StartingValue(ParameterTable parameters, string id)
        {
            var v = parameters.GetInt(id);
            if (v < 0) throw new ParameterFormatException(id + " = " + v + " must not be negative.");
            return v;
        }

        private readonly string _parametersDigest = "none";

        public ulong Seed { get; }

        /// <summary>
        /// The Faction whose turn it is (TRN-006, TRN-007), or null for a state without a map (no campaign, no turns).
        /// Only EndTurnCommand changes it.
        /// </summary>
        public string? ActiveFaction { get; internal set; }

        /// <summary>Count of Faction turns started, from 1 for the first player's turn. 0 without a campaign.</summary>
        public int TurnNumber { get; internal set; }

        /// <summary>
        /// The phase of the active Faction's turn (TRN-005). Between commands it is always Actions: start of turn and
        /// income run inside the command that begins the turn, because they need no player choice.
        /// </summary>
        public TurnPhase Phase { get; internal set; } = TurnPhase.Actions;

        /// <summary>Test seam: called as each phase is entered. Not part of the state and not hashed.</summary>
        internal Action<TurnPhase, string>? PhaseObserver { get; set; }

        /// <summary>The Faction after <paramref name="faction"/> in turn order (Factions are sorted by id), wrapping round.</summary>
        internal string NextFaction(string faction)
        {
            for (var i = 0; i < Factions.Count; i++)
                if (Factions[i].Id == faction) return Factions[(i + 1) % Factions.Count].Id;
            throw new InvalidOperationException("Unknown Faction " + faction);
        }

        /// <summary>The map this game is played on, or null for a state made without one (tests of the core only).</summary>
        public HexMap? Map { get; }

        /// <summary>How income is worked out (RES-002, RES-005), or null when there is no map. Fixed for a game; not hashed.</summary>
        public IncomeRules? IncomeRules { get; }

        /// <summary>
        /// Capacity, cost per hex, supply state and the Slow cap, built once at game start from the Section types and
        /// parameters (DLV-014), or null when there is no map. Fixed for a game; its parameters are hashed as a digest.
        /// </summary>
        public Supply? Supply { get; }

        /// <summary>The Slow movement cap (P-SUP-10), or null when there is no map. Fixed for a game; not hashed.</summary>
        public MovementRules? MovementRules { get; }

        /// <summary>Territory radii (MAP-016, MAP-027), built once at game start, or null when there is no map. Fixed for a game; not hashed (its parameters are in the digest).</summary>
        public TerritoryRules? Territory { get; }

        /// <summary>Brigade size limit (P-BRG-01, BRG-021), built once at game start, or null when there is no map. Fixed for a game; not hashed (its parameter is in the digest).</summary>
        public BrigadeRules? BrigadeRules { get; }

        /// <summary>The FOB upgrade cost (P-MAP-03), built once at game start, or null when there is no map. Fixed for a game; not hashed (its parameter is in the digest).</summary>
        public FobRules? FobRules { get; }

        /// <summary>The Section types in use, or null when there is no map.</summary>
        public SectionTypeTable? SectionTypes { get; }

        /// <summary>Factions sorted by id.</summary>
        public IReadOnlyList<FactionState> Factions { get; } = new List<FactionState>();

        /// <summary>Locations sorted by id.</summary>
        public IReadOnlyList<LocationState> Locations { get; } = new List<LocationState>();

        /// <summary>Brigades sorted by id. Ids start at 1.</summary>
        public IReadOnlyList<Brigade> Brigades => _brigades;
        private List<Brigade> _brigades = new List<Brigade>();

        /// <summary>The id the next new Brigade takes. It only goes up, so a disbanded Brigade's id is never used again (BRG-023, TEC-008). In the hash.</summary>
        internal int NextBrigadeId => _nextBrigadeId;
        private int _nextBrigadeId = 1;

        /// <summary>Takes the next never-used Brigade id. Whatever forms a Brigade later calls this. May only be called from inside a command's Apply (DLV-013).</summary>
        internal int TakeBrigadeId() => _nextBrigadeId++;

        /// <summary>Adds a formed Brigade. Whatever forms Brigades later calls this; until then tests use it to place extra Brigades. Take its id from <see cref="TakeBrigadeId"/>.</summary>
        internal void AddBrigade(Brigade brigade) => _brigades.Add(brigade);

        /// <summary>
        /// Removes one Section from a Brigade (the shared step for transfers and desertion). A Brigade left with 0 Sections
        /// is disbanded (BRG-023): it leaves <see cref="Brigades"/>, so its hex is free. Nothing else is touched: a pending
        /// battle that names the Brigade stays as it is, because no rule says what happens to it. The officer's return to
        /// the roster is a later Story. After forming there is no minimum size (BRG-020).
        /// May only be called from inside a command's Apply (DLV-013).
        /// </summary>
        internal void RemoveSectionFromBrigade(Brigade brigade, int sectionIndex)
        {
            brigade.RemoveSection(sectionIndex);
            if (brigade.Sections.Count == 0) _brigades.Remove(brigade);
        }

        /// <summary>
        /// Battles that have started and not been resolved (TRN-003, TRN-008, TRN-036). CC1 only starts them (DEC-103), so
        /// none ends. In the hash, so replay must reproduce it (TEC-008).
        /// </summary>
        public IReadOnlyList<PendingBattle> Battles => _battles;
        private readonly List<PendingBattle> _battles = new List<PendingBattle>();
        internal void StartBattle(int attackerId, int defenderId) => _battles.Add(new PendingBattle(attackerId, defenderId));

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
            var reason = CheckTurn(command) ?? command.Validate(this);
            if (reason != null) return CommandResult.Reject(reason);
            var canonical = Encoding.UTF8.GetBytes(CommandJson.Canonical(command));
            command.Apply(this);
            Log.Append(command);
            LogDigest = Sha256(LogDigest, canonical);
            return CommandResult.Accept();
        }

        /// <summary>
        /// TRN-006 and TRN-005: in a campaign only the active Faction may send commands, and a command that names a
        /// phase is accepted only in that phase. Runs before the command's own Validate. A state without a campaign has no turns.
        /// </summary>
        private string? CheckTurn(ICommand command)
        {
            // Keyed on the map, not on ActiveFaction: every campaign state has a map, so clearing ActiveFaction later
            // (e.g. at game over) must not switch the gate off.
            if (Map == null) return null;
            if (command.Issuer != ActiveFaction)
                return "It is not " + (command.Issuer.Length == 0 ? "an unnamed Faction" : command.Issuer) + "'s turn; the active Faction is " + ActiveFaction;
            if (command.RequiredPhase != null && command.RequiredPhase != Phase)
                return "This command is only allowed in the " + command.RequiredPhase + " phase; the phase is " + Phase;
            return null;
        }

        /// <summary>
        /// Rebuilds a state from a seed and a log. Throws <see cref="ReplayDivergenceException"/> if an entry
        /// is out of order (step is not its position) or a command is rejected.
        /// </summary>
        public static GameState Replay(ulong seed, CommandLog log, HexMap? map = null, SectionTypeTable? sectionTypes = null, ParameterTable? parameters = null, CardTable? cards = null)
        {
            var state = map == null ? new GameState(seed) : new GameState(seed, map, sectionTypes ?? throw new ArgumentNullException(nameof(sectionTypes)), parameters ?? throw new ArgumentNullException(nameof(parameters)), cards);
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
            if (Map != null) sb.Append("parameters=").Append(_parametersDigest).Append('\n');
            sb.Append("sectiontypes=").Append(SectionTypes == null ? "none" : Hex(Sha256(Encoding.UTF8.GetBytes(SectionTypes.CanonicalText())))).Append('\n');
            foreach (var f in Factions)
            {
                sb.Append("faction=").Append(f.Id).Append(" reserve=").Append(f.Reserve.ToString(CultureInfo.InvariantCulture))
                  .Append(" cp=").Append(f.Cp.ToString(CultureInfo.InvariantCulture));
                // Written only once the Faction has upgraded an FOB, so a game with none hashes as before (MAP-012).
                if (f.FobUpgrades > 0) sb.Append(" fobupgrades=").Append(f.FobUpgrades.ToString(CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            // Written only for a game with Card data, so a game without Cards hashes as before (CRD-003, TEC-019: ids only).
            if (_cards != null)
            {
                sb.Append("cards=").Append(Hex(Sha256(Encoding.UTF8.GetBytes(_cards.CanonicalText())))).Append('\n');
                foreach (var f in Factions)
                    sb.Append("cardstate=").Append(f.Id).Append(" deck=").Append(string.Join(",", f.Deck))
                      .Append(" hand=").Append(string.Join(",", f.Hand)).Append(" discard=").Append(string.Join(",", f.Discard)).Append('\n');
            }
            foreach (var l in Locations)
            {
                sb.Append("location=").Append(l.Id).Append(" owner=").Append(l.Owner ?? "-");
                // Written only for an upgraded location (MAP-012), so a game with none hashes as before.
                if (l.Upgraded) sb.Append(" fob");
                // Written only while a capture is pending (TRN-009), so a game with none hashes as before.
                if (l.Capturing != null) sb.Append(" capturing=").Append(l.Capturing).Append(" brigade=").Append(l.CapturingBrigade?.ToString(CultureInfo.InvariantCulture) ?? "-");
                sb.Append('\n');
            }
            if (Map != null) sb.Append("nextbrigade=").Append(_nextBrigadeId.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var b in Brigades)
            {
                sb.Append("brigade=").Append(b.Id.ToString(CultureInfo.InvariantCulture)).Append(" faction=").Append(b.Faction)
                  .Append(" sp=").Append(b.SP.ToString(CultureInfo.InvariantCulture)).Append(" sections=");
                for (var i = 0; i < b.Sections.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(b.Sections[i].TypeId).Append(':').Append(b.Sections[i].Health.ToString(CultureInfo.InvariantCulture));
                }
                var (bc, br) = b.Position.ToOffset();
                sb.Append(" at=").Append(bc.ToString(CultureInfo.InvariantCulture)).Append(',').Append(br.ToString(CultureInfo.InvariantCulture));
                // Written only once the Brigade has moved, so a game with no moves hashes as before (SUP-024 Slow cap).
                if (b.MovedHexes > 0)
                    sb.Append(" moved=").Append(b.MovedTurn.ToString(CultureInfo.InvariantCulture)).Append(':').Append(b.MovedHexes.ToString(CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            // Written only while a battle is pending, so a game with none hashes as before (TRN-008).
            foreach (var battle in Battles)
                sb.Append("battle=").Append(battle.AttackerId.ToString(CultureInfo.InvariantCulture)).Append(',').Append(battle.DefenderId.ToString(CultureInfo.InvariantCulture)).Append('\n');
            if (ActiveFaction != null)
                sb.Append("turn=").Append(TurnNumber.ToString(CultureInfo.InvariantCulture)).Append(" active=").Append(ActiveFaction)
                  .Append(" phase=").Append(Phase.ToString()).Append('\n');
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
