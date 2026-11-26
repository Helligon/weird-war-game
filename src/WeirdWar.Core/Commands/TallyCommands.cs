using System;
using System.Collections.Generic;
using WeirdWar.Core.Serialization;
using WeirdWar.Core.Simulation;

namespace WeirdWar.Core.Commands
{
    /// <summary>
    /// Trivial command that proves the framework (DLV-013). Adds Amount to the state's tally; rejected
    /// if the tally would go below zero. Real commands arrive with later Stories.
    /// </summary>
    public sealed class AddToTallyCommand : ICommand, IEquatable<AddToTallyCommand>
    {
        public const string TypeName = "addToTally";

        public AddToTallyCommand(long amount) { Amount = amount; }

        public long Amount { get; }
        public string Type => TypeName;

        public string? Validate(GameState state) =>
            state.Tally + Amount < 0 ? "Tally cannot go below zero" : null;

        public void Apply(GameState state) { state.Tally += Amount; }

        public Dictionary<string, object?> ToJson() => new Dictionary<string, object?> { ["amount"] = Amount };

        public static AddToTallyCommand FromJson(IReadOnlyDictionary<string, object?> json) =>
            new AddToTallyCommand(CommandRegistry.RequireLong(json, "amount"));

        public bool Equals(AddToTallyCommand? other) => other != null && other.Amount == Amount;
        public override bool Equals(object? obj) => Equals(obj as AddToTallyCommand);
        public override int GetHashCode() => Amount.GetHashCode();
    }

    /// <summary>
    /// Trivial command that draws from the state's single random source (TEC-008, TEC-012): adds a roll
    /// of 1..Sides to the tally. Rejected if Sides is below 2.
    /// </summary>
    public sealed class RollIntoTallyCommand : ICommand, IEquatable<RollIntoTallyCommand>
    {
        public const string TypeName = "rollIntoTally";

        public RollIntoTallyCommand(int sides) { Sides = sides; }

        public int Sides { get; }
        public string Type => TypeName;

        public string? Validate(GameState state) => Sides < 2 ? "A die needs at least 2 sides" : null;

        public void Apply(GameState state) { state.Tally += state.Random.NextInt(Sides) + 1; }

        public Dictionary<string, object?> ToJson() => new Dictionary<string, object?> { ["sides"] = (long)Sides };

        public static RollIntoTallyCommand FromJson(IReadOnlyDictionary<string, object?> json)
        {
            var sides = CommandRegistry.RequireLong(json, "sides");
            if (sides < int.MinValue || sides > int.MaxValue) throw new JsonException("Field 'sides' out of range");
            return new RollIntoTallyCommand((int)sides);
        }

        public bool Equals(RollIntoTallyCommand? other) => other != null && other.Sides == Sides;
        public override bool Equals(object? obj) => Equals(obj as RollIntoTallyCommand);
        public override int GetHashCode() => Sides;
    }
}
