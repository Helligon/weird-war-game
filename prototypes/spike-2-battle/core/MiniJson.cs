using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WeirdWar.Spike2.Battle.Core
{
    /// <summary>
    /// A small JSON reader. Gives Dictionary&lt;string, object&gt; for objects, List&lt;object&gt; for arrays,
    /// string, long, double, bool and null. Hand-written so the core has no package dependencies
    /// and works unchanged inside Unity.
    /// </summary>
    internal sealed class MiniJson
    {
        private readonly string _s;
        private int _i;

        private MiniJson(string s) { _s = s; }

        public static object? Parse(string text)
        {
            var p = new MiniJson(text);
            p.SkipSpace();
            var value = p.ReadValue();
            p.SkipSpace();
            if (p._i != p._s.Length)
            {
                throw p.Fail("unexpected text after the JSON value");
            }
            return value;
        }

        private DataLoadException Fail(string what) => new DataLoadException("Invalid JSON at position " + _i + ": " + what);

        private void SkipSpace()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
        }

        private object? ReadValue()
        {
            if (_i >= _s.Length) throw Fail("unexpected end");
            var c = _s[_i];
            if (c == '{') return ReadObject();
            if (c == '[') return ReadArray();
            if (c == '"') return ReadString();
            if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
            if (Take("true")) return true;
            if (Take("false")) return false;
            if (Take("null")) return null;
            throw Fail("unexpected character '" + c + "'");
        }

        private bool Take(string word)
        {
            if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) return false;
            _i += word.Length;
            return true;
        }

        private Dictionary<string, object?> ReadObject()
        {
            var result = new Dictionary<string, object?>();
            _i++;
            SkipSpace();
            if (Peek('}')) { _i++; return result; }
            while (true)
            {
                SkipSpace();
                if (!Peek('"')) throw Fail("expected a property name");
                var key = ReadString();
                SkipSpace();
                if (!Peek(':')) throw Fail("expected ':'");
                _i++;
                SkipSpace();
                if (result.ContainsKey(key)) throw Fail("duplicate property '" + key + "'");
                result[key] = ReadValue();
                SkipSpace();
                if (Peek(',')) { _i++; continue; }
                if (Peek('}')) { _i++; return result; }
                throw Fail("expected ',' or '}'");
            }
        }

        private List<object?> ReadArray()
        {
            var result = new List<object?>();
            _i++;
            SkipSpace();
            if (Peek(']')) { _i++; return result; }
            while (true)
            {
                SkipSpace();
                result.Add(ReadValue());
                SkipSpace();
                if (Peek(',')) { _i++; continue; }
                if (Peek(']')) { _i++; return result; }
                throw Fail("expected ',' or ']'");
            }
        }

        private string ReadString()
        {
            var sb = new StringBuilder();
            _i++;
            while (_i < _s.Length)
            {
                var c = _s[_i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (_i >= _s.Length) break;
                var e = _s[_i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    default: throw Fail("unsupported escape '\\" + e + "'");
                }
            }
            throw Fail("unterminated string");
        }

        private object ReadNumber()
        {
            var start = _i;
            if (_s[_i] == '-') _i++;
            while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+' || _s[_i] == '-')) _i++;
            var text = _s.Substring(start, _i - start);
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l)) return l;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
            throw Fail("bad number '" + text + "'");
        }

        private bool Peek(char c) => _i < _s.Length && _s[_i] == c;
    }
}
