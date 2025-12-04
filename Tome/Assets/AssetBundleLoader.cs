using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInExPaths = BepInEx.Paths;
using Jotunn.Utils;
using UnityEngine;

namespace Tome.Assets
{
    /// <summary>
    /// Handles loading and caching of AssetBundles for Tome items.
    /// Supports both embedded resources and external file paths.
    /// </summary>
    public static class AssetBundleLoader
    {
        private static readonly Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>();
        private static readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Default bundle name for Tome's built-in assets.
        /// </summary>
        public const string DefaultBundleName = "tome_assets";

        /// <summary>
        /// Directory name for external asset bundles.
        /// </summary>
        public const string BundleDirectory = "AssetBundles";

        /// <summary>
        /// Loads an AssetBundle from embedded resources.
        /// </summary>
        /// <param name="bundleName">Name of the bundle (without extension)</param>
        /// <param name="assembly">Assembly containing the embedded resource (defaults to Tome assembly)</param>
        /// <returns>The loaded AssetBundle, or null if not found</returns>
        public static AssetBundle LoadEmbeddedBundle(string bundleName, Assembly assembly = null)
        {
            if (string.IsNullOrEmpty(bundleName))
                return null;

            // Check cache first
            if (LoadedBundles.TryGetValue(bundleName, out var cached))
                return cached;

            assembly ??= typeof(AssetBundleLoader).Assembly;

            try
            {
                var bundle = AssetUtils.LoadAssetBundleFromResources(bundleName, assembly);
                if (bundle != null)
                {
                    LoadedBundles[bundleName] = bundle;
                    Plugin.Log?.LogInfo($"Loaded embedded AssetBundle: {bundleName}");
                    return bundle;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to load embedded bundle '{bundleName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads an AssetBundle from an external file.
        /// Searches in BepInEx/plugins/Tome/AssetBundles/ and BepInEx/config/Tome/AssetBundles/
        /// </summary>
        /// <param name="bundleName">Name of the bundle file (without extension)</param>
        /// <returns>The loaded AssetBundle, or null if not found</returns>
        public static AssetBundle LoadExternalBundle(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
                return null;

            // Check cache first
            if (LoadedBundles.TryGetValue(bundleName, out var cached))
                return cached;

            // Search paths
            var searchPaths = new[]
            {
                Path.Combine(BepInExPaths.PluginPath, "Tome", BundleDirectory, bundleName),
                Path.Combine(BepInExPaths.ConfigPath, "Tome", BundleDirectory, bundleName),
                Path.Combine(BepInExPaths.PluginPath, "Tome", BundleDirectory, $"{bundleName}.bundle"),
                Path.Combine(BepInExPaths.ConfigPath, "Tome", BundleDirectory, $"{bundleName}.bundle"),
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var bundle = AssetBundle.LoadFromFile(path);
                        if (bundle != null)
                        {
                            LoadedBundles[bundleName] = bundle;
                            Plugin.Log?.LogInfo($"Loaded external AssetBundle: {path}");
                            return bundle;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogWarning($"Failed to load bundle from '{path}': {ex.Message}");
                    }
                }
            }

            Plugin.Log?.LogDebug($"External bundle '{bundleName}' not found in search paths");
            return null;
        }

        /// <summary>
        /// Loads an AssetBundle, trying embedded first, then external.
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <returns>The loaded AssetBundle, or null if not found</returns>
        public static AssetBundle LoadBundle(string bundleName)
        {
            // Try embedded first
            var bundle = LoadEmbeddedBundle(bundleName);
            if (bundle != null)
                return bundle;

            // Try external
            return LoadExternalBundle(bundleName);
        }

        /// <summary>
        /// Gets a prefab from a loaded AssetBundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <param name="prefabName">Name of the prefab asset</param>
        /// <returns>The prefab GameObject, or null if not found</returns>
        public static GameObject GetPrefab(string bundleName, string prefabName)
        {
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(prefabName))
                return null;

            var cacheKey = $"{bundleName}:{prefabName}";

            // Check cache
            if (PrefabCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var bundle = LoadBundle(bundleName);
            if (bundle == null)
                return null;

            try
            {
                var prefab = bundle.LoadAsset<GameObject>(prefabName);
                if (prefab != null)
                {
                    PrefabCache[cacheKey] = prefab;
                    Plugin.Log?.LogDebug($"Loaded prefab '{prefabName}' from bundle '{bundleName}'");
                    return prefab;
                }

                Plugin.Log?.LogWarning($"Prefab '{prefabName}' not found in bundle '{bundleName}'");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to load prefab '{prefabName}' from bundle '{bundleName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets a sprite from a loaded AssetBundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle</param>
        /// <param name="spriteName">Name of the sprite asset</param>
        /// <returns>The Sprite, or null if not found</returns>
        public static Sprite GetSprite(string bundleName, string spriteName)
        {
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(spriteName))
                return null;

            var cacheKey = $"{bundleName}:{spriteName}";

            // Check cache
            if (SpriteCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var bundle = LoadBundle(bundleName);
            if (bundle == null)
                return null;

            try
            {
                var sprite = bundle.LoadAsset<Sprite>(spriteName);
                if (sprite != null)
                {
                    SpriteCache[cacheKey] = sprite;
                    Plugin.Log?.LogDebug($"Loaded sprite '{spriteName}' from bundle '{bundleName}'");
                    return sprite;
                }

                // Try loading as Texture2D and converting to Sprite
                var texture = bundle.LoadAsset<Texture2D>(spriteName);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    SpriteCache[cacheKey] = sprite;
                    Plugin.Log?.LogDebug($"Loaded texture '{spriteName}' as sprite from bundle '{bundleName}'");
                    return sprite;
                }

                Plugin.Log?.LogWarning($"Sprite/Texture '{spriteName}' not found in bundle '{bundleName}'");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to load sprite '{spriteName}' from bundle '{bundleName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads a sprite from an embedded PNG resource using Jotunn's AssetUtils.
        /// </summary>
        /// <param name="resourceName">Name of the embedded resource (e.g., "icon_rifttoken.png")</param>
        /// <returns>The Sprite, or null if not found</returns>
        public static Sprite LoadEmbeddedSprite(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
                return null;

            // Check cache
            if (SpriteCache.TryGetValue(resourceName, out var cached))
                return cached;

            try
            {
                // Use Jotunn's AssetUtils to load embedded texture
                var texture = AssetUtils.LoadTexture(resourceName);
                if (texture != null)
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    SpriteCache[resourceName] = sprite;
                    Plugin.Log?.LogDebug($"Loaded embedded sprite: {resourceName}");
                    return sprite;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"Failed to load embedded sprite '{resourceName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads a sprite from an external PNG file.
        /// Searches in BepInEx/plugins/Tome/Icons/ and BepInEx/config/Tome/Icons/
        /// </summary>
        /// <param name="fileName">Name of the PNG file (with or without extension)</param>
        /// <returns>The Sprite, or null if not found</returns>
        public static Sprite LoadExternalSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            // Ensure .png extension
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                fileName += ".png";

            // Check cache
            if (SpriteCache.TryGetValue(fileName, out var cached))
                return cached;

            var searchPaths = new[]
            {
                Path.Combine(BepInExPaths.PluginPath, "Tome", "Icons", fileName),
                Path.Combine(BepInExPaths.ConfigPath, "Tome", "Icons", fileName),
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        // Use Jotunn's AssetUtils to load texture from file path
                        var texture = AssetUtils.LoadTexture(path, relativePath: false);
                        if (texture != null)
                        {
                            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                            SpriteCache[fileName] = sprite;
                            Plugin.Log?.LogInfo($"Loaded external sprite: {path}");
                            return sprite;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogWarning($"Failed to load sprite from '{path}': {ex.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a bundle is loaded.
        /// </summary>
        public static bool IsBundleLoaded(string bundleName) => LoadedBundles.ContainsKey(bundleName);

        /// <summary>
        /// Gets all loaded bundle names.
        /// </summary>
        public static IEnumerable<string> GetLoadedBundles() => LoadedBundles.Keys;

        /// <summary>
        /// Unloads all loaded AssetBundles.
        /// </summary>
        /// <param name="unloadAllLoadedObjects">If true, also unloads all objects loaded from the bundles</param>
        public static void UnloadAll(bool unloadAllLoadedObjects = false)
        {
            foreach (var bundle in LoadedBundles.Values)
            {
                bundle?.Unload(unloadAllLoadedObjects);
            }

            LoadedBundles.Clear();
            PrefabCache.Clear();
            SpriteCache.Clear();

            Plugin.Log?.LogInfo("Unloaded all AssetBundles");
        }
    }
}
