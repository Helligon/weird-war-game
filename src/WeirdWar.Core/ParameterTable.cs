using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace WeirdWar.Core
{
    /// <summary>
    /// The only way the core reads tunable values (DLV-014). Values are named by parameter ID.
    /// A missing ID is an error; there are no defaults.
    ///
    /// Data file format (line based, UTF-8):
    ///   - Blank lines and lines starting with '#' are ignored.
    ///   - An entry is "P-XXX-NN = value", where XXX is 2 to 4 capital letters and NN is two digits.
    ///   - Anything after the first '#' on an entry line is a comment.
    ///   - A comment that starts with "placeholder (open)" or "placeholder (proposed)" marks the
    ///     value as a placeholder for a parameter that is not decided yet (see <see cref="IsPlaceholder"/>).
    ///   - Any other comment that mentions "placeholder" is a format error, so a mistyped marker is never read as "decided".
    ///   - Each ID may appear once. Malformed lines and duplicate IDs throw <see cref="ParameterFormatException"/>.
    /// Shipped file: Data/parameters.txt (copied to the output directory next to the assembly).
    /// </summary>
    public sealed class ParameterTable
    {
        private static readonly Regex IdPattern = new Regex("^P-[A-Z]{2,4}-[0-9]{2}$", RegexOptions.Compiled);
        private static readonly Regex PlaceholderPattern =
            new Regex(@"^placeholder \((open|proposed)\)", RegexOptions.Compiled);

        private readonly Dictionary<string, string> _values;
        private readonly HashSet<string> _placeholders;

        private ParameterTable(Dictionary<string, string> values, HashSet<string> placeholders)
        {
            _values = values;
            _placeholders = placeholders;
        }

        public IEnumerable<string> Ids => _values.Keys;

        /// <summary>
        /// SHA-256 (lowercase hex) of the table as text, one "ID=value" line per parameter sorted by ID (TEC-008).
        /// Comments and placeholder markers are not part of it, so the digest changes only when a value does.
        /// </summary>
        public string Digest()
        {
            var ids = new List<string>(_values.Keys);
            ids.Sort(StringComparer.Ordinal);
            var sb = new System.Text.StringBuilder();
            foreach (var id in ids) sb.Append(id).Append('=').Append(_values[id]).Append('\n');
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
                var hex = new System.Text.StringBuilder();
                foreach (var b in bytes) hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }

        public static ParameterTable Parse(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var placeholders = new HashSet<string>(StringComparer.Ordinal);
            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var lineNo = i + 1;
                var line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                var comment = "";
                var hash = line.IndexOf('#');
                if (hash >= 0)
                {
                    comment = line.Substring(hash + 1).Trim();
                    line = line.Substring(0, hash).Trim();
                }

                var eq = line.IndexOf('=');
                if (eq < 0)
                    throw new ParameterFormatException($"Line {lineNo}: expected 'P-XXX-NN = value'.");
                var id = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                if (!IdPattern.IsMatch(id))
                    throw new ParameterFormatException($"Line {lineNo}: '{id}' is not a parameter ID of the form P-XXX-NN.");
                if (value.Length == 0)
                    throw new ParameterFormatException($"Line {lineNo}: {id} has no value.");
                if (values.ContainsKey(id))
                    throw new ParameterFormatException($"Line {lineNo}: duplicate parameter ID {id}.");

                values[id] = value;
                if (PlaceholderPattern.IsMatch(comment)) placeholders.Add(id);
                else if (comment.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new ParameterFormatException(
                        $"Line {lineNo}: {id} has a comment mentioning 'placeholder' that is not exactly '# placeholder (open)' or '# placeholder (proposed)'.");
            }
            return new ParameterTable(values, placeholders);
        }

        public static ParameterTable Load(string path) => Parse(File.ReadAllText(path));

        /// <summary>
        /// Loads Data/parameters.txt shipped next to the Core assembly (works for dotnet and the tests).
        /// Hosts where Assembly.Location is empty (for example Unity IL2CPP) should call <see cref="Parse"/> or
        /// <see cref="Load"/> instead. How Unity ships this file is decided in the first Unity Feature (TEC-016).
        /// </summary>
        public static ParameterTable LoadShipped()
        {
            var location = typeof(ParameterTable).Assembly.Location;
            if (string.IsNullOrEmpty(location))
                throw new InvalidOperationException(
                    "Cannot locate the shipped parameter file: Assembly.Location is empty. Use ParameterTable.Parse or Load.");
            return Load(Path.Combine(Path.GetDirectoryName(location)!, "Data", "parameters.txt"));
        }

        public bool IsPlaceholder(string id)
        {
            Get(id);
            return _placeholders.Contains(id);
        }

        public string Get(string id)
        {
            if (!_values.TryGetValue(id, out var value))
                throw new MissingParameterException(id);
            return value;
        }

        public int GetInt(string id)
        {
            var text = Get(id);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                throw new ParameterFormatException($"{id} = '{text}' is not an integer.");
            return v;
        }

        /// <summary>
        /// Reads a parameter written as "key=number" pairs (TEC-017): single spaces between pairs and the keys exactly in
        /// <paramref name="keys"/> order (the order the parameter's row documents). Each number is a whole number.
        /// A missing ID throws <see cref="MissingParameterException"/>; any other mismatch throws
        /// <see cref="ParameterFormatException"/> naming the ID.
        /// </summary>
        public Dictionary<string, int> GetKeyedInts(string id, params string[] keys)
        {
            var text = Get(id);
            var pairs = text.Split(' ');
            var expected = string.Join(" ", keys) + " in that order, single-spaced";
            if (pairs.Length != keys.Length)
                throw new ParameterFormatException($"{id} = '{text}' must be key=number pairs for: {expected}.");
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < keys.Length; i++)
            {
                var kv = pairs[i].Split('=');
                if (kv.Length != 2 || kv[0] != keys[i]
                    || !int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                    throw new ParameterFormatException($"{id} = '{text}' must be key=number pairs for: {expected}.");
                result[kv[0]] = v;
            }
            return result;
        }

        public double GetDouble(string id)
        {
            var text = Get(id);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                || double.IsNaN(v) || double.IsInfinity(v))
                throw new ParameterFormatException($"{id} = '{text}' is not a finite number.");
            return v;
        }

        /// <summary>Exact decimal value (invariant culture), for thresholds compared against integers (TEC-008).</summary>
        public decimal GetDecimal(string id)
        {
            var text = Get(id);
            if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new ParameterFormatException($"{id} = '{text}' is not a finite number.");
            return v;
        }
    }

    /// <summary>A requested parameter ID has no entry in the data file (DLV-014).</summary>
    public sealed class MissingParameterException : Exception
    {
        public string ParameterId { get; }

        public MissingParameterException(string id)
            : base($"Missing parameter {id}: no entry in the parameter data file.")
        {
            ParameterId = id;
        }
    }

    /// <summary>The parameter data file is malformed (DLV-014).</summary>
    public sealed class ParameterFormatException : Exception
    {
        public ParameterFormatException(string message) : base(message) { }
    }
}
