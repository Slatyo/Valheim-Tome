using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tome.Registry;
using UnityEngine;

namespace Tome.Items
{
    /// <summary>
    /// Loads item definitions from JSON files and embedded resources.
    /// </summary>
    public static class ItemLoader
    {
        /// <summary>
        /// Loads items from an embedded resource JSON file.
        /// </summary>
        /// <param name="resourceName">The embedded resource name (e.g., "Tome.Data.items.json")</param>
        /// <returns>Number of items loaded</returns>
        public static int LoadFromEmbeddedResource(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    Plugin.Log?.LogWarning($"[Tome] Embedded resource not found: {resourceName}");
                    return 0;
                }

                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                return LoadFromJson(json);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Tome] Failed to load embedded resource '{resourceName}': {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Loads items from a JSON file path.
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <returns>Number of items loaded</returns>
        public static int LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Plugin.Log?.LogWarning($"[Tome] Items file not found: {filePath}");
                    return 0;
                }

                string json = File.ReadAllText(filePath);
                return LoadFromJson(json);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Tome] Failed to load items file '{filePath}': {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Loads items from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <returns>Number of items loaded</returns>
        public static int LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return 0;

            try
            {
                var data = SimpleJson.Parse(json);
                if (data == null)
                {
                    Plugin.Log?.LogError("[Tome] Failed to parse items JSON");
                    return 0;
                }

                var items = data.GetArray("Items");
                if (items == null)
                {
                    Plugin.Log?.LogWarning("[Tome] No 'Items' array in JSON");
                    return 0;
                }

                int loaded = 0;
                foreach (var itemObj in items)
                {
                    try
                    {
                        JsonObject jsonObj = itemObj as JsonObject;
                        if (jsonObj == null && itemObj is string itemStr)
                        {
                            jsonObj = new JsonObject(itemStr);
                        }

                        if (jsonObj != null)
                        {
                            var def = ParseItemDefinition(jsonObj);
                            if (def != null && TomeRegistry.Instance.Register(def))
                            {
                                loaded++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogError($"[Tome] Failed to parse item: {ex.Message}");
                    }
                }

                Plugin.Log?.LogInfo($"[Tome] Loaded {loaded} item definitions from JSON");
                return loaded;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Tome] Failed to load items from JSON: {ex.Message}");
                return 0;
            }
        }

        private static ItemDefinition ParseItemDefinition(JsonObject obj)
        {
            string prefabName = obj.GetString("PrefabName");
            if (string.IsNullOrEmpty(prefabName))
            {
                Plugin.Log?.LogWarning("[Tome] Item missing PrefabName, skipping");
                return null;
            }

            var def = new ItemDefinition(prefabName)
            {
                DisplayName = obj.GetString("DisplayName") ?? $"$item_{prefabName.ToLowerInvariant()}",
                Description = obj.GetString("Description") ?? $"$item_{prefabName.ToLowerInvariant()}_desc",
                Category = ParseCategory(obj.GetString("Category")),
                MaxStack = obj.GetInt("MaxStack", 1),
                Weight = obj.GetFloat("Weight", 1f),
                Tradeable = obj.GetBool("Tradeable", true),
                Consumable = obj.GetBool("Consumable", false),
                OnUseAbility = obj.GetString("OnUseAbility"),
                IconPath = obj.GetString("Icon"),
                CloneFrom = obj.GetString("CloneFrom"),
                Value = obj.GetInt("Value", 0),
                Teleportable = obj.GetBool("Teleportable", true),

                // Asset bundle properties
                Bundle = obj.GetString("Bundle"),
                BundlePrefab = obj.GetString("BundlePrefab"),
                BundleIcon = obj.GetString("BundleIcon")
            };

            // Parse flags
            var flagsArray = obj.GetArray("Flags");
            if (flagsArray != null)
            {
                foreach (var flagStr in flagsArray)
                {
                    if (Enum.TryParse<ItemFlags>(flagStr.ToString(), true, out var flag))
                    {
                        def.Flags |= flag;
                    }
                }
            }

            return def;
        }

        private static TomeCategory ParseCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return TomeCategory.Misc;

            if (Enum.TryParse<TomeCategory>(category, true, out var result))
                return result;

            return TomeCategory.Misc;
        }
    }

    /// <summary>
    /// Simple JSON parser for item definitions.
    /// No external dependencies - uses manual parsing.
    /// </summary>
    internal static class SimpleJson
    {
        /// <summary>
        /// Parses a JSON string into a JsonObject.
        /// </summary>
        public static JsonObject Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            return new JsonObject(json);
        }
    }

    /// <summary>
    /// Simple JSON object wrapper.
    /// </summary>
    internal class JsonObject
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a JsonObject by parsing the given JSON string.
        /// </summary>
        public JsonObject(string json)
        {
            ParseJsonToDictionary(json);
        }

        private void ParseJsonToDictionary(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}"))
                return;

            // Remove outer braces
            json = json.Substring(1, json.Length - 2);

            // Parse key-value pairs
            int depth = 0;
            int arrayDepth = 0;
            int start = 0;
            bool inString = false;
            string currentKey = null;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                // Handle escape sequences properly
                if (c == '\\' && inString && i + 1 < json.Length)
                {
                    i++; // Skip next character
                    continue;
                }

                if (c == '"')
                    inString = !inString;

                if (!inString)
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                    else if (c == '[') arrayDepth++;
                    else if (c == ']') arrayDepth--;
                    else if (c == ':' && depth == 0 && arrayDepth == 0 && currentKey == null)
                    {
                        currentKey = ExtractStringValue(json.Substring(start, i - start));
                        start = i + 1;
                    }
                    else if (c == ',' && depth == 0 && arrayDepth == 0)
                    {
                        if (currentKey != null)
                        {
                            string value = json.Substring(start, i - start).Trim();
                            _data[currentKey] = ParseValue(value);
                            currentKey = null;
                        }
                        start = i + 1;
                    }
                }
            }

            // Handle last pair
            if (currentKey != null)
            {
                string value = json.Substring(start).Trim();
                _data[currentKey] = ParseValue(value);
            }
        }

        private static string ExtractStringValue(string s)
        {
            s = s.Trim();
            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
                return s.Substring(1, s.Length - 2);
            return s;
        }

        private object ParseValue(string value)
        {
            value = value.Trim();

            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Substring(1, value.Length - 2);

            if (value == "true") return true;
            if (value == "false") return false;
            if (value == "null") return null;

            if (value.StartsWith("["))
                return ParseArray(value);

            if (value.StartsWith("{"))
                return new JsonObject(value);

            if (int.TryParse(value, out int intVal))
                return intVal;

            if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                return floatVal;

            return value;
        }

        private List<object> ParseArray(string json)
        {
            var result = new List<object>();
            json = json.Trim();

            if (!json.StartsWith("[") || !json.EndsWith("]"))
                return result;

            json = json.Substring(1, json.Length - 2);
            if (string.IsNullOrWhiteSpace(json))
                return result;

            int depth = 0;
            int arrayDepth = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                // Handle escape sequences properly
                if (c == '\\' && inString && i + 1 < json.Length)
                {
                    i++; // Skip next character
                    continue;
                }

                if (c == '"')
                    inString = !inString;

                if (!inString)
                {
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                    else if (c == '[') arrayDepth++;
                    else if (c == ']') arrayDepth--;
                    else if (c == ',' && depth == 0 && arrayDepth == 0)
                    {
                        string element = json.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(element))
                            result.Add(ParseValue(element));
                        start = i + 1;
                    }
                }
            }

            // Handle last element
            string lastElement = json.Substring(start).Trim();
            if (!string.IsNullOrEmpty(lastElement))
                result.Add(ParseValue(lastElement));

            return result;
        }

        public string GetString(string key)
        {
            if (_data.TryGetValue(key, out var value))
                return value?.ToString();
            return null;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is int i) return i;
                if (int.TryParse(value?.ToString(), out int result))
                    return result;
            }
            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is float f) return f;
                if (value is int i) return i;
                if (float.TryParse(value?.ToString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float result))
                    return result;
            }
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is bool b) return b;
                if (bool.TryParse(value?.ToString(), out bool result))
                    return result;
            }
            return defaultValue;
        }

        public List<object> GetArray(string key)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is List<object> list)
                    return list;
            }
            return null;
        }

        public JsonObject GetObject(string key)
        {
            if (_data.TryGetValue(key, out var value))
            {
                if (value is JsonObject obj)
                    return obj;
            }
            return null;
        }
    }
}
