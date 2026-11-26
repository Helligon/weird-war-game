using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WeirdWar.Core.Serialization
{
    /// <summary>Thrown when JSON text is malformed or a value cannot be written.</summary>
    public sealed class JsonException : Exception
    {
        public JsonException(string message) : base(message) { }
    }

    /// <summary>
    /// Small JSON reader and writer (no external package, TEC-016). The reader returns plain objects:
    /// null, bool, long, double, string, List&lt;object?&gt; and Dictionary&lt;string, object?&gt;.
    /// The writer also takes int, ulong and any IEnumerable of supported values (e.g. List&lt;long&gt;).
    /// Whole numbers beyond long are an error on read; write 64-bit seeds as decimal strings.
    /// The writer is canonical: object keys are sorted ordinally, no whitespace, so output is
    /// identical on every machine (TEC-008).
    /// </summary>
    public static class Json
    {
        /// <summary>Maximum nesting of arrays and objects the reader accepts.</summary>
        public const int MaxDepth = 64;

        public static string Write(object? value)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value);
            return sb.ToString();
        }

        public static object? Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var p = new Parser(text);
            p.SkipBom();
            p.SkipWs();
            var v = p.ReadValue();
            p.SkipWs();
            if (!p.AtEnd) throw p.Error("Unexpected trailing characters");
            return v;
        }

        private static void WriteValue(StringBuilder sb, object? v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case ulong ul: sb.Append(ul.ToString(CultureInfo.InvariantCulture)); break;
                case double d:
                    if (double.IsNaN(d) || double.IsInfinity(d)) throw new JsonException("NaN and Infinity cannot be written");
                    var s = d.ToString("R", CultureInfo.InvariantCulture);
                    sb.Append(s);
                    // Keep doubles distinguishable from integers so they read back as doubles.
                    if (s.IndexOfAny(new[] { '.', 'E', 'e' }) < 0) sb.Append(".0");
                    break;
                case string str: WriteString(sb, str); break;
                case IReadOnlyDictionary<string, object?> dict:
                    var keys = new List<string>(dict.Keys);
                    keys.Sort(StringComparer.Ordinal);
                    sb.Append('{');
                    for (int k = 0; k < keys.Count; k++)
                    {
                        if (k > 0) sb.Append(',');
                        WriteString(sb, keys[k]);
                        sb.Append(':');
                        WriteValue(sb, dict[keys[k]]);
                    }
                    sb.Append('}');
                    break;
                case System.Collections.IEnumerable list:
                    sb.Append('[');
                    bool first = true;
                    foreach (var item in list)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteValue(sb, item);
                    }
                    sb.Append(']');
                    break;
                default: throw new JsonException("Cannot write type " + v.GetType().Name);
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string _t;
            private int _p;
            private int _depth;
            public Parser(string t) { _t = t; }
            public bool AtEnd => _p >= _t.Length;

            public JsonException Error(string msg)
            {
                int line = 1, col = 1;
                for (int i = 0; i < _p && i < _t.Length; i++)
                {
                    if (_t[i] == '\n') { line++; col = 1; } else col++;
                }
                return new JsonException(msg + " at line " + line + ", column " + col);
            }

            public void SkipBom()
            {
                if (_t.Length > 0 && _t[0] == '\uFEFF') _p = 1;
            }

            public void SkipWs()
            {
                while (_p < _t.Length && (_t[_p] == ' ' || _t[_p] == '\t' || _t[_p] == '\n' || _t[_p] == '\r')) _p++;
            }

            public object? ReadValue()
            {
                if (AtEnd) throw Error("Unexpected end of input");
                char c = _t[_p];
                if (c == '{' || c == '[')
                {
                    if (++_depth > MaxDepth) throw Error("Nesting deeper than " + MaxDepth);
                    object result = c == '{' ? (object)ReadObject() : ReadArray();
                    _depth--;
                    return result;
                }
                if (c == '"') return ReadString();
                if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                if (Match("true")) return true;
                if (Match("false")) return false;
                if (Match("null")) return null;
                throw Error("Unexpected character '" + c + "'");
            }

            private bool Match(string word)
            {
                if (string.CompareOrdinal(_t, _p, word, 0, word.Length) != 0) return false;
                _p += word.Length;
                return true;
            }

            private Dictionary<string, object?> ReadObject()
            {
                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                _p++;
                SkipWs();
                if (!AtEnd && _t[_p] == '}') { _p++; return result; }
                while (true)
                {
                    SkipWs();
                    if (AtEnd || _t[_p] != '"') throw Error("Expected object key");
                    var key = ReadString();
                    SkipWs();
                    if (AtEnd || _t[_p] != ':') throw Error("Expected ':'");
                    _p++;
                    SkipWs();
                    if (result.ContainsKey(key)) throw Error("Duplicate key '" + key + "'");
                    result[key] = ReadValue();
                    SkipWs();
                    if (AtEnd) throw Error("Unterminated object");
                    if (_t[_p] == ',') { _p++; continue; }
                    if (_t[_p] == '}') { _p++; return result; }
                    throw Error("Expected ',' or '}'");
                }
            }

            private List<object?> ReadArray()
            {
                var result = new List<object?>();
                _p++;
                SkipWs();
                if (!AtEnd && _t[_p] == ']') { _p++; return result; }
                while (true)
                {
                    SkipWs();
                    result.Add(ReadValue());
                    SkipWs();
                    if (AtEnd) throw Error("Unterminated array");
                    if (_t[_p] == ',') { _p++; continue; }
                    if (_t[_p] == ']') { _p++; return result; }
                    throw Error("Expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                _p++; // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw Error("Unterminated string");
                    char c = _t[_p++];
                    if (c == '"') return sb.ToString();
                    if (c < 0x20) throw Error("Control character in string");
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("Unterminated escape");
                    char e = _t[_p++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (_p + 4 > _t.Length) throw Error("Bad \\u escape");
                            if (!int.TryParse(_t.Substring(_p, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                                throw Error("Bad \\u escape");
                            sb.Append((char)code);
                            _p += 4;
                            break;
                        default: throw Error("Bad escape '\\" + e + "'");
                    }
                }
            }

            private object ReadNumber()
            {
                int start = _p;
                bool isFloat = false;
                if (_t[_p] == '-') _p++;
                int digitsStart = _p;
                while (_p < _t.Length && _t[_p] >= '0' && _t[_p] <= '9') _p++;
                if (_p == digitsStart) throw Error("Bad number");
                if (_t[digitsStart] == '0' && _p - digitsStart > 1) throw Error("Leading zero in number");
                if (_p < _t.Length && _t[_p] == '.')
                {
                    isFloat = true;
                    _p++;
                    int f = _p;
                    while (_p < _t.Length && _t[_p] >= '0' && _t[_p] <= '9') _p++;
                    if (_p == f) throw Error("Bad number");
                }
                if (_p < _t.Length && (_t[_p] == 'e' || _t[_p] == 'E'))
                {
                    isFloat = true;
                    _p++;
                    if (_p < _t.Length && (_t[_p] == '+' || _t[_p] == '-')) _p++;
                    int x = _p;
                    while (_p < _t.Length && _t[_p] >= '0' && _t[_p] <= '9') _p++;
                    if (_p == x) throw Error("Bad number");
                }
                var text = _t.Substring(start, _p - start);
                if (!isFloat)
                {
                    if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l)) return l;
                    _p = start;
                    throw Error("Integer out of range");
                }
                var d = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (double.IsInfinity(d) || double.IsNaN(d))
                {
                    _p = start;
                    throw Error("Number out of range");
                }
                return d;
            }
        }
    }
}
