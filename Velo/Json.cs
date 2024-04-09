using System;
using System.Collections.Generic;

namespace Velo
{
    public abstract class JsonElement
    {
        public abstract string ToString(bool whiteSpace, int depth = 0);

        private static void SkipSpaces(string value, ref int offset)
        {
            while (offset < value.Length && (value[offset] == ' ' || value[offset] == '\n' || value[offset] == '\r' || value[offset] == '\0'))
                offset++;
        }

        private static string ReadString(string value, ref int offset)
        {
            int start = offset;
            offset++;
            if (offset >= value.Length)
                return "";
            while (offset < value.Length && (value[offset] != '\"' || value[offset - 1] == '\\'))
                offset++;
            offset++;
            return value.Substring(start + 1, offset - start - 2).Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        private static bool IsNumberChar(char c)
        {
            return c >= '0' && c <= '9' || c == '-' || c == '.';
        }

        private static string ReadNumber(string value, ref int offset)
        {
            if (offset >= value.Length)
                return "0";
            int start = offset;
            while (offset < value.Length && IsNumberChar(value[offset]))
                offset++;
            return value.Substring(start, offset - start);
        }

        public static JsonElement FromString(string value)
        {
            int offset = 0;
            return FromString(value, ref offset);
        }

        private static JsonElement FromString(string value, ref int offset)
        {
            SkipSpaces(value, ref offset);

            if (offset >= value.Length)
                return new JsonNull();
            if (value[offset] == '{')
            {
                JsonObject jsonObject = new JsonObject(new List<KeyValuePair<string, JsonElement>>());
                offset++;
                SkipSpaces(value, ref offset);
                if (offset >= value.Length || value[offset] == '}')
                {
                    offset++;
                    return jsonObject;
                }
                while (true)
                {
                    string key = ReadString(value, ref offset);
                    SkipSpaces(value, ref offset);
                    offset++;
                    SkipSpaces(value, ref offset);
                    jsonObject.AddElement(key, FromString(value, ref offset));
                    SkipSpaces(value, ref offset);
                    if (offset >= value.Length || value[offset] == '}')
                    {
                        offset++;
                        return jsonObject;
                    }
                    offset++;
                    SkipSpaces(value, ref offset);
                }
            }
            else if (value[offset] == '[')
            {
                JsonArray jsonArray = new JsonArray(new List<JsonElement>());
                offset++;
                SkipSpaces(value, ref offset);
                if (offset >= value.Length || value[offset] == ']')
                    return jsonArray;
                while (true)
                {
                    jsonArray.AddElement(FromString(value, ref offset));
                    SkipSpaces(value, ref offset);
                    if (offset >= value.Length || value[offset] == ']')
                    {
                        offset++;
                        return jsonArray;
                    }
                    offset++;
                    SkipSpaces(value, ref offset);
                }
            }
            else if (value[offset] == '\"')
            {
                return new JsonString(ReadString(value, ref offset));
            }
            else if (IsNumberChar(value[offset]))
            {
                return new JsonDecimal(ReadNumber(value, ref offset));
            }
            else if (value[offset] == 't')
            {
                offset += "true".Length;
                return new JsonBoolean(true);
            }
            else if (value[offset] == 'f')
            {
                offset += "false".Length;
                return new JsonBoolean(false);
            }
          
            offset += "null".Length;
            return new JsonNull();
        }
    }

    public class JsonDecimal : JsonElement
    {
        public string value;

        public JsonDecimal(string value)
        {
            this.value = value;
        }

        public JsonDecimal(int value)
        {
            this.value = value.ToString();
        }

        public JsonDecimal(float value)
        {
            this.value = value.ToString();
        }

        public override string ToString(bool whiteSpace, int depth = 0)
        {
            return value;
        }
    }

    public class JsonBoolean : JsonElement
    {
        public bool value;

        public JsonBoolean(bool value)
        {
            this.value = value;
        }

        public override string ToString(bool whiteSpace, int depth = 0)
        {
            return value ? "true" : "false";
        }
    }

    public class JsonString : JsonElement
    {
        public string value;

        public JsonString(string value)
        {
            this.value = value;
        }

        public override string ToString(bool whiteSpace, int depth = 0)
        {
            return "\"" + value.
                Replace("\\", "\\\\").
                Replace("\"", "\\\"").
                Replace("\n", "\\n") + "\"";
        }
    }

    public class JsonNull : JsonElement
    {
        public JsonNull()
        {

        }

        public override string ToString(bool whiteSpace, int depth = 0)
        {
            return "null";
        }
    }

    public class JsonArray : JsonElement
    {
        public List<JsonElement> value;

        public JsonArray(List<JsonElement> value)
        {
            this.value = value;
        }

        public JsonArray(int capacity = 0)
        {
            value = new List<JsonElement>(capacity);
        }

        public JsonArray AddElement(JsonElement value)
        {
            this.value.Add(value);
            return this;
        }

        public JsonArray AddDecimal(string value)
        {
            this.value.Add(new JsonDecimal(value));
            return this;
        }

        public JsonArray AddDecimal(int value)
        {
            this.value.Add(new JsonDecimal(value));
            return this;
        }

        public JsonArray AddDecimal(float value)
        {
            this.value.Add(new JsonDecimal(value));
            return this;
        }

        public JsonArray AddBoolean(bool value)
        {
            this.value.Add(new JsonBoolean(value));
            return this;
        }

        public JsonArray AddString(string value)
        {
            this.value.Add(new JsonString(value));
            return this;
        }

        public JsonArray AddNull()
        {
            value.Add(new JsonNull());
            return this;
        }

        public JsonArray AddArray(List<JsonElement> value)
        {
            this.value.Add(new JsonArray(value));
            return this;
        }

        public JsonArray AddObject(List<KeyValuePair<string, JsonElement>> value)
        {
            this.value.Add(new JsonObject(value));
            return this;
        }

        public JsonArray AddElementIf(JsonElement value, bool condition)
        {
            if (condition)
                this.value.Add(value);
            return this;
        }

        public JsonArray AddDecimalIf(string value, bool condition)
        {
            if (condition)
                this.value.Add(new JsonDecimal(value));
            return this;
        }

        public JsonArray AddDecimalIf(int value, bool condition)
        {
            if (condition)
                this.value.Add(new JsonDecimal(value));
            return this;
        }

        public JsonArray AddDecimalIf(float value, bool condition)
        {
            if (condition)
                this.value.Add(new JsonDecimal(value));
            return this;
        }

        public JsonArray AddBooleanIf(bool value, bool condition)
        {
            if (condition)
                this.value.Add(new JsonBoolean(value));
            return this;
        }

        public JsonArray AddStringIf(string value, bool condition)
        {
            if (condition)
                this.value.Add(new JsonString(value));
            return this;
        }

        public JsonArray AddNullIf(bool condition)
        {
            if (condition)
                value.Add(new JsonNull());
            return this;
        }

        public JsonArray AddArrayIf(List<JsonElement> value, bool condition)
        {
            if (condition)
                this.value.Add(new JsonArray(value));
            return this;
        }

        public JsonArray AddObjectIf(List<KeyValuePair<string, JsonElement>> value, bool condition)
        {
            if (condition)
                this.value.Add(new JsonObject(value));
            return this;
        }

        public override string ToString(bool whiteSpace, int depth = 0)
        {
            if (value.Count == 0)
                return "{}";

            string space = "";
            if (whiteSpace)
            {
                for (int i = 0; i < depth; i++)
                    space += "    ";
            }

            string valStr = "[";
            if (whiteSpace)
                valStr += "\n";
            for (int i = 0; i < value.Count; i++)
            {
                if (value[i] == null)
                    continue;
                if (whiteSpace)
                    valStr += space + "    ";
                valStr += value[i].ToString(whiteSpace, depth + 1) + ",";
                if (whiteSpace)
                    valStr += "\n";
            }
            if (whiteSpace)
                valStr = valStr.Remove(valStr.Length - 2, 2);
            else
                valStr = valStr.Remove(valStr.Length - 1, 1);
            if (whiteSpace)
                valStr += "\n" + space;
            valStr += "]";
            return valStr;
        }
    }

    public class JsonObject : JsonElement
    {
        public List<KeyValuePair<string, JsonElement>> value;

        public JsonObject(List<KeyValuePair<string, JsonElement>> value)
        {
            this.value = value;
        }

        public JsonObject(int capacity = 0)
        {
            value = new List<KeyValuePair<string, JsonElement>>(capacity);
        }

        public JsonObject AddElement(string key, JsonElement value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, value));
            return this;
        }

        public JsonObject AddDecimal(string key, string value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonDecimal(value)));
            return this;
        }

        public JsonObject AddDecimal(string key, int value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonDecimal(value)));
            return this;
        }

        public JsonObject AddDecimal(string key, float value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonDecimal(value)));
            return this;
        }

        public JsonObject AddBoolean(string key, bool value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonBoolean(value)));
            return this;
        }

        public JsonObject AddString(string key, string value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonString(value)));
            return this;
        }

        public JsonObject AddNull(string key)
        {
            value.Add(new KeyValuePair<string, JsonElement>(key, new JsonNull()));
            return this;
        }

        public JsonObject AddArray(string key, List<JsonElement> value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonArray(value)));
            return this;
        }

        public JsonObject AddObject(string key, List<KeyValuePair<string, JsonElement>> value)
        {
            this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonObject(value)));
            return this;
        }

        public JsonObject AddElementIf(string key, JsonElement value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, value));
            return this;
        }

        public JsonObject AddDecimalIf(string key, string value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonDecimal(value)));
            return this;
        }

        public JsonObject AddDecimalIf(string key, int value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonDecimal(value)));
            return this;
        }

        public JsonObject AddDecimalIf(string key, float value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonDecimal(value)));
            return this;
        }

        public JsonObject AddBooleanIf(string key, bool value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonBoolean(value)));
            return this;
        }

        public JsonObject AddStringIf(string key, string value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonString(value)));
            return this;
        }

        public JsonObject AddNullIf(string key, bool condition)
        {
            if (condition)
                value.Add(new KeyValuePair<string, JsonElement>(key, new JsonNull()));
            return this;
        }

        public JsonObject AddArrayIf(string key, List<JsonElement> value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonArray(value)));
            return this;
        }

        public JsonObject AddObjectIf(string key, List<KeyValuePair<string, JsonElement>> value, bool condition)
        {
            if (condition)
                this.value.Add(new KeyValuePair<string, JsonElement>(key, new JsonObject(value)));
            return this;
        }

        public JsonElement Get(string key)
        {
            return value.Find((elem) => elem.Key == key).Value;
        }

        public void DoWithValue(string key, Action<JsonElement> action)
        {
            JsonElement value = Get(key);
            if (value != null)
                action(value);
        }

        public T DoWithValue<T>(string key, Func<JsonElement, T> func)
        {
            JsonElement value = Get(key);
            if (value != null)
                return func(value);
            return default(T);
        }

        public override string ToString(bool whiteSpace, int depth = 0)
        {
            if (value.Count == 0)
                return "[]";

            string space = "";
            if (whiteSpace)
            {
                for (int i = 0; i < depth; i++)
                    space += "    ";
            }

            string valStr = "{";
            if (whiteSpace)
                valStr += "\n";
            for (int i = 0; i < value.Count; i++)
            {
                if (whiteSpace)
                    valStr += space + "    ";
                valStr += "\"" + value[i].Key.Replace("\"", "\\\"") + "\":";
                if (whiteSpace)
                    valStr += " ";
                valStr += value[i].Value.ToString(whiteSpace, depth + 1);
                valStr += ",";
                if (whiteSpace)
                    valStr += "\n";
            }
            if (whiteSpace)
                valStr = valStr.Remove(valStr.Length - 2, 2);
            else
                valStr = valStr.Remove(valStr.Length - 1, 1);
            if (whiteSpace)
                valStr += "\n" + space;
            valStr += "}";
            return valStr;
        }
    }
}
