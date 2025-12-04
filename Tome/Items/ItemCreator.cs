using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Tome.Assets;
using Tome.Registry;
using UnityEngine;

namespace Tome.Items
{
    /// <summary>
    /// Creates and registers item prefabs from ItemDefinitions.
    /// Supports both cloning vanilla items and loading custom prefabs from AssetBundles.
    /// </summary>
    public static class ItemCreator
    {
        private static readonly Dictionary<TomeCategory, string> DefaultCloneSource = new Dictionary<TomeCategory, string>
        {
            { TomeCategory.Currency, "Coins" },
            { TomeCategory.CraftingMaterial, "Coal" },
            { TomeCategory.Consumable, "MeadHealthMinor" },
            { TomeCategory.Rune, "Ruby" },
            { TomeCategory.Scroll, "TrophyDeer" },
            { TomeCategory.Token, "Coins" },
            { TomeCategory.Trophy, "TrophyDeer" },
            { TomeCategory.QuestItem, "Ruby" },
            { TomeCategory.Misc, "Coal" }
        };

        /// <summary>
        /// Creates prefabs for all registered item definitions.
        /// Call this in OnVanillaPrefabsAvailable.
        /// </summary>
        public static int CreateAllItems()
        {
            int created = 0;
            int fromBundle = 0;
            int cloned = 0;

            var definitions = TomeRegistry.Instance.GetAll();

            foreach (var def in definitions)
            {
                try
                {
                    if (CreateItem(def))
                    {
                        created++;
                        if (def.HasCustomPrefab)
                            fromBundle++;
                        else
                            cloned++;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"Failed to create item '{def.PrefabName}': {ex.Message}");
                }
            }

            Plugin.Log?.LogInfo($"Created {created} item prefabs ({fromBundle} from bundles, {cloned} cloned)");
            return created;
        }

        /// <summary>
        /// Creates a single item prefab from a definition.
        /// Automatically chooses between bundle prefab or vanilla clone based on definition.
        /// </summary>
        /// <param name="def">The item definition</param>
        /// <returns>True if created successfully</returns>
        public static bool CreateItem(ItemDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.PrefabName))
                return false;

            // Load icon first (shared between both creation methods)
            LoadIcon(def);

            // Try to create from AssetBundle if specified
            if (def.HasCustomPrefab)
            {
                return CreateFromBundle(def);
            }

            // Fall back to cloning vanilla item
            return CreateFromClone(def);
        }

        /// <summary>
        /// Loads the icon for an item definition from various sources.
        /// </summary>
        private static void LoadIcon(ItemDefinition def)
        {
            // Already has icon loaded
            if (def.Icon != null)
                return;

            // Try BundleIcon from AssetBundle first
            if (!string.IsNullOrEmpty(def.BundleIcon) && !string.IsNullOrEmpty(def.Bundle))
            {
                def.Icon = AssetBundleLoader.GetSprite(def.Bundle, def.BundleIcon);
                if (def.Icon != null)
                {
                    Plugin.Log?.LogDebug($"Loaded icon '{def.BundleIcon}' from bundle '{def.Bundle}' for {def.PrefabName}");
                    return;
                }
            }

            // Try IconPath
            if (!string.IsNullOrEmpty(def.IconPath))
            {
                // Check if it's a bundle reference (format: "bundlename:spritename")
                if (def.IconPath.Contains(":"))
                {
                    var parts = def.IconPath.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        def.Icon = AssetBundleLoader.GetSprite(parts[0], parts[1]);
                        if (def.Icon != null)
                        {
                            Plugin.Log?.LogDebug($"Loaded icon from bundle reference '{def.IconPath}' for {def.PrefabName}");
                            return;
                        }
                    }
                }

                // Try as external PNG file
                def.Icon = AssetBundleLoader.LoadExternalSprite(def.IconPath);
                if (def.Icon != null)
                {
                    Plugin.Log?.LogDebug($"Loaded external icon '{def.IconPath}' for {def.PrefabName}");
                    return;
                }

                // Try as embedded resource
                string embeddedPath = $"Tome.Assets.Icons.{def.IconPath}";
                if (!def.IconPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    embeddedPath += ".png";

                def.Icon = AssetBundleLoader.LoadEmbeddedSprite(embeddedPath);
                if (def.Icon != null)
                {
                    Plugin.Log?.LogDebug($"Loaded embedded icon '{embeddedPath}' for {def.PrefabName}");
                }
            }
        }

        /// <summary>
        /// Creates an item from a custom prefab in an AssetBundle.
        /// </summary>
        private static bool CreateFromBundle(ItemDefinition def)
        {
            var prefab = AssetBundleLoader.GetPrefab(def.Bundle, def.EffectiveBundlePrefab);
            if (prefab == null)
            {
                Plugin.Log?.LogWarning($"Prefab '{def.EffectiveBundlePrefab}' not found in bundle '{def.Bundle}' for item '{def.PrefabName}', falling back to clone");
                return CreateFromClone(def);
            }

            var config = new ItemConfig
            {
                Name = def.DisplayName,
                Description = def.Description,
                Weight = def.Weight,
                StackSize = def.MaxStack
            };

            if (def.Icon != null)
            {
                config.Icons = new[] { def.Icon };
            }

            try
            {
                // Clone the prefab to avoid modifying the original
                var prefabClone = UnityEngine.Object.Instantiate(prefab);
                prefabClone.name = def.PrefabName;

                var customItem = new CustomItem(prefabClone, fixReference: true, config);

                if (customItem.ItemDrop == null)
                {
                    Plugin.Log?.LogError($"Failed to create CustomItem from bundle prefab for '{def.PrefabName}' - ItemDrop is null");
                    UnityEngine.Object.Destroy(prefabClone);
                    return CreateFromClone(def);
                }

                // Apply properties
                ApplyItemProperties(customItem.ItemDrop, def);

                // Register with Jotunn
                ItemManager.Instance.AddItem(customItem);

                // Register prefab with Tome registry
                TomeRegistry.Instance.RegisterPrefab(def.PrefabName, customItem.ItemPrefab);

                Plugin.Log?.LogDebug($"Created item: {def.PrefabName} (from bundle {def.Bundle}:{def.EffectiveBundlePrefab})");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Exception creating item '{def.PrefabName}' from bundle: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Creates an item by cloning a vanilla item.
        /// </summary>
        private static bool CreateFromClone(ItemDefinition def)
        {
            // Determine what to clone from
            string cloneFrom = def.CloneFrom;
            if (string.IsNullOrEmpty(cloneFrom))
            {
                cloneFrom = DefaultCloneSource.TryGetValue(def.Category, out string defaultClone)
                    ? defaultClone
                    : "Coal";
            }

            var config = new ItemConfig
            {
                Name = def.DisplayName,
                Description = def.Description,
                Weight = def.Weight,
                StackSize = def.MaxStack
            };

            if (def.Icon != null)
            {
                config.Icons = new[] { def.Icon };
            }

            try
            {
                var customItem = new CustomItem(def.PrefabName, cloneFrom, config);

                if (customItem.ItemDrop == null)
                {
                    Plugin.Log?.LogError($"Failed to create CustomItem for '{def.PrefabName}' - ItemDrop is null");
                    return false;
                }

                // Apply properties
                ApplyItemProperties(customItem.ItemDrop, def);

                // Register with Jotunn
                ItemManager.Instance.AddItem(customItem);

                // Register prefab with Tome registry
                TomeRegistry.Instance.RegisterPrefab(def.PrefabName, customItem.ItemPrefab);

                Plugin.Log?.LogDebug($"Created item: {def.PrefabName} (cloned from {cloneFrom})");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Exception creating item '{def.PrefabName}': {ex}");
                return false;
            }
        }

        /// <summary>
        /// Applies common item properties from definition to ItemDrop.
        /// </summary>
        private static void ApplyItemProperties(ItemDrop itemDrop, ItemDefinition def)
        {
            var shared = itemDrop.m_itemData.m_shared;

            shared.m_name = def.DisplayName;
            shared.m_description = def.Description;
            shared.m_weight = def.Weight;
            shared.m_maxStackSize = def.MaxStack;
            shared.m_value = def.Value;
            shared.m_teleportable = def.Teleportable;

            // Set item type based on category
            if (def.Consumable)
            {
                shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            }
            else if (def.Category == TomeCategory.Trophy)
            {
                shared.m_itemType = ItemDrop.ItemData.ItemType.Trophy;
            }
            else
            {
                shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            }

            // Apply trade/quest flags
            if (!def.Tradeable || def.HasFlag(ItemFlags.NoTrade))
            {
                shared.m_questItem = true; // Quest items can't be traded
            }
        }

        /// <summary>
        /// Creates an item with an externally provided prefab.
        /// Use this when other mods want to register items with custom prefabs.
        /// </summary>
        /// <param name="def">The item definition</param>
        /// <param name="prefab">The custom prefab (will be cloned)</param>
        /// <returns>True if created successfully</returns>
        public static bool CreateItemFromPrefab(ItemDefinition def, GameObject prefab)
        {
            if (def == null || prefab == null)
                return false;

            // Load icon if not set
            LoadIcon(def);

            var config = new ItemConfig
            {
                Name = def.DisplayName,
                Description = def.Description,
                Weight = def.Weight,
                StackSize = def.MaxStack
            };

            if (def.Icon != null)
            {
                config.Icons = new[] { def.Icon };
            }

            try
            {
                // Clone the prefab
                var prefabClone = UnityEngine.Object.Instantiate(prefab);
                prefabClone.name = def.PrefabName;

                var customItem = new CustomItem(prefabClone, fixReference: true, config);

                if (customItem.ItemDrop != null)
                {
                    ApplyItemProperties(customItem.ItemDrop, def);
                }

                ItemManager.Instance.AddItem(customItem);
                TomeRegistry.Instance.RegisterPrefab(def.PrefabName, customItem.ItemPrefab);

                Plugin.Log?.LogDebug($"Created item from external prefab: {def.PrefabName}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Exception creating item from prefab '{def.PrefabName}': {ex}");
                return false;
            }
        }
    }
}
