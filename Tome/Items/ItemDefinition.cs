using System;
using UnityEngine;

namespace Tome.Items
{
    /// <summary>
    /// Defines a Tome item's properties.
    /// This is the configuration class used to register items with TomeRegistry.
    /// </summary>
    public class ItemDefinition
    {
        /// <summary>
        /// Unique prefab name for this item (e.g., "RiftToken").
        /// </summary>
        public string PrefabName { get; set; }

        /// <summary>
        /// Localization key for display name (e.g., "$item_rifttoken").
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Localization key for description (e.g., "$item_rifttoken_desc").
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Item category for sorting and filtering.
        /// </summary>
        public TomeCategory Category { get; set; } = TomeCategory.Misc;

        /// <summary>
        /// Maximum stack size.
        /// </summary>
        public int MaxStack { get; set; } = 1;

        /// <summary>
        /// Item weight per unit.
        /// </summary>
        public float Weight { get; set; } = 1f;

        /// <summary>
        /// Can this item be traded between players?
        /// </summary>
        public bool Tradeable { get; set; } = true;

        /// <summary>
        /// Is this a consumable item?
        /// </summary>
        public bool Consumable { get; set; } = false;

        /// <summary>
        /// Prime ability ID to trigger when this item is consumed.
        /// Only applies if Consumable is true.
        /// </summary>
        public string OnUseAbility { get; set; }

        /// <summary>
        /// Icon sprite for UI display (set at runtime).
        /// </summary>
        public Sprite Icon { get; set; }

        /// <summary>
        /// Path or name of icon to load.
        /// Can be:
        /// - External file name: "rift_token.png" (searches Icons folder)
        /// - Bundle reference: "bundle:sprite_name" (loads from AssetBundle)
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Behavior flags for this item.
        /// </summary>
        public ItemFlags Flags { get; set; } = ItemFlags.None;

        /// <summary>
        /// Base item to clone from (e.g., "Coins" for currency-like items).
        /// Used when no custom prefab is specified.
        /// </summary>
        public string CloneFrom { get; set; }

        /// <summary>
        /// Value for trading/selling.
        /// </summary>
        public int Value { get; set; } = 0;

        /// <summary>
        /// Teleportable - can this item go through portals?
        /// </summary>
        public bool Teleportable { get; set; } = true;

        #region Asset Bundle Properties

        /// <summary>
        /// Name of the AssetBundle containing this item's prefab.
        /// If null/empty, the item is created by cloning a vanilla item.
        /// Example: "tome_assets", "my_custom_bundle"
        /// </summary>
        public string Bundle { get; set; }

        /// <summary>
        /// Name of the prefab asset within the AssetBundle.
        /// If null/empty, defaults to PrefabName.
        /// Example: "RiftToken_Prefab", "MyItem"
        /// </summary>
        public string BundlePrefab { get; set; }

        /// <summary>
        /// Name of the icon sprite/texture within the AssetBundle.
        /// If specified, overrides IconPath for bundle-based loading.
        /// Example: "RiftToken_Icon", "icon_rifttoken"
        /// </summary>
        public string BundleIcon { get; set; }

        /// <summary>
        /// Gets whether this item uses a custom prefab from an AssetBundle.
        /// </summary>
        public bool HasCustomPrefab => !string.IsNullOrEmpty(Bundle);

        /// <summary>
        /// Gets the effective prefab name to load from the bundle.
        /// Returns BundlePrefab if set, otherwise PrefabName.
        /// </summary>
        public string EffectiveBundlePrefab => !string.IsNullOrEmpty(BundlePrefab) ? BundlePrefab : PrefabName;

        #endregion

        /// <summary>
        /// Custom data for mod-specific extensions.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> CustomData { get; set; }
            = new System.Collections.Generic.Dictionary<string, object>();

        /// <summary>
        /// Creates a new item definition.
        /// </summary>
        public ItemDefinition() { }

        /// <summary>
        /// Creates a new item definition with prefab name.
        /// </summary>
        /// <param name="prefabName">Unique prefab name</param>
        public ItemDefinition(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName))
                throw new ArgumentException("Prefab name cannot be null or empty", nameof(prefabName));

            PrefabName = prefabName;
            DisplayName = $"$item_{prefabName.ToLowerInvariant()}";
            Description = $"$item_{prefabName.ToLowerInvariant()}_desc";
        }

        /// <summary>
        /// Creates an item definition for a custom prefab from an AssetBundle.
        /// </summary>
        /// <param name="prefabName">Unique prefab name</param>
        /// <param name="bundleName">Name of the AssetBundle</param>
        /// <param name="bundlePrefab">Name of prefab in bundle (optional, defaults to prefabName)</param>
        public ItemDefinition(string prefabName, string bundleName, string bundlePrefab = null)
            : this(prefabName)
        {
            Bundle = bundleName;
            BundlePrefab = bundlePrefab;
        }

        /// <summary>
        /// Checks if this item has a specific flag.
        /// </summary>
        public bool HasFlag(ItemFlags flag) => (Flags & flag) == flag;

        /// <summary>
        /// Validates the item definition.
        /// </summary>
        /// <param name="error">Error message if invalid</param>
        /// <returns>True if valid</returns>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(PrefabName))
            {
                error = "PrefabName is required";
                return false;
            }

            if (MaxStack < 1)
            {
                error = "MaxStack must be at least 1";
                return false;
            }

            if (Weight < 0)
            {
                error = "Weight cannot be negative";
                return false;
            }

            if (Consumable && string.IsNullOrEmpty(OnUseAbility))
            {
                // Consumable without ability is allowed (vanilla consumption behavior)
            }

            error = null;
            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (HasCustomPrefab)
                return $"ItemDefinition({PrefabName} from {Bundle}:{EffectiveBundlePrefab})";
            return $"ItemDefinition({PrefabName})";
        }
    }
}
