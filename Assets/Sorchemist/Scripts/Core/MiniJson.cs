// Küçük JSON okuyucu/yazıcı: sunucu yanıtlarını Dictionary/List olarak okur, istekleri yazar.
// (Unity'nin JsonUtility'si serbest biçimli JSON okuyamadığı için.)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sorchemist.Core
{
    public static class MiniJson
    {
        // ------------------------------------------------------------ okuma
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int i = 0;
            try
            {
                var v = ReadValue(json, ref i);
                return v;
            }
            catch (Exception) { return null; }
        }

        static void Skip(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object ReadValue(string s, ref int i)
        {
            Skip(s, ref i);
            char c = s[i];
            if (c == '{') return ReadObject(s, ref i);
            if (c == '[') return ReadArray(s, ref i);
            if (c == '"') return ReadString(s, ref i);
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            var num = s.Substring(start, i - start);
            long l;
            if (num.IndexOfAny(new[] { '.', 'e', 'E' }) < 0 && long.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) return l;
            return double.Parse(num, CultureInfo.InvariantCulture);
        }

        static Dictionary<string, object> ReadObject(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++;
            Skip(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                Skip(s, ref i);
                var key = ReadString(s, ref i);
                Skip(s, ref i);
                i++; // :
                d[key] = ReadValue(s, ref i);
                Skip(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++; // }
                return d;
            }
        }

        static List<object> ReadArray(string s, ref int i)
        {
            var l = new List<object>();
            i++;
            Skip(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(ReadValue(s, ref i));
                Skip(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++; // ]
                return l;
            }
        }

        static string ReadString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // "
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            i++;
            return sb.ToString();
        }

        // ------------------------------------------------------------ yazma
        public static string Serialize(object o)
        {
            var sb = new StringBuilder();
            Write(sb, o);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object o)
        {
            if (o == null) { sb.Append("null"); return; }
            if (o is string) { WriteString(sb, (string)o); return; }
            if (o is bool) { sb.Append((bool)o ? "true" : "false"); return; }
            if (o is IDictionary)
            {
                sb.Append('{');
                bool first = true;
                foreach (DictionaryEntry kv in (IDictionary)o)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, kv.Key.ToString());
                    sb.Append(':');
                    Write(sb, kv.Value);
                }
                sb.Append('}');
                return;
            }
            if (o is IEnumerable)
            {
                sb.Append('[');
                bool first = true;
                foreach (var v in (IEnumerable)o) { if (!first) sb.Append(','); first = false; Write(sb, v); }
                sb.Append(']');
                return;
            }
            if (o is float || o is double) { sb.Append(Convert.ToDouble(o).ToString("R", CultureInfo.InvariantCulture)); return; }
            sb.Append(Convert.ToString(o, CultureInfo.InvariantCulture));
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }

    /// <summary>Sunucudan gelen JSON nesnesini rahat okumak için ince sarmalayıcı.</summary>
    public class JNode
    {
        public readonly object Raw;
        public JNode(object raw) { Raw = raw; }

        public static JNode From(string json) { return new JNode(MiniJson.Parse(json)); }

        public JNode this[string key]
        {
            get
            {
                var d = Raw as Dictionary<string, object>;
                object v;
                return new JNode(d != null && d.TryGetValue(key, out v) ? v : null);
            }
        }

        public JNode this[int index]
        {
            get { var l = Raw as List<object>; return new JNode(l != null && index >= 0 && index < l.Count ? l[index] : null); }
        }

        public bool IsNull { get { return Raw == null; } }
        public int Count { get { var l = Raw as List<object>; return l != null ? l.Count : 0; } }
        public string Str { get { return Raw == null ? null : Convert.ToString(Raw, CultureInfo.InvariantCulture); } }
        public long Long { get { try { return Raw == null ? 0 : Convert.ToInt64(Raw, CultureInfo.InvariantCulture); } catch { return 0; } } }
        public int Int { get { return (int)Long; } }
        public bool Bool { get { return Raw is bool ? (bool)Raw : Long != 0; } }

        public IEnumerable<JNode> Items
        {
            get { var l = Raw as List<object>; if (l != null) foreach (var v in l) yield return new JNode(v); }
        }
    }
}
