using System;
using System.Collections.Generic;
using System.Linq;
using Tome.Items;
using UnityEngine;

namespace Tome.Registry
{
    /// <summary>
    /// Central registry for all Tome items.
    /// Other mods query this registry to access shared items.
    /// </summary>
    public class TomeRegistry
    {
        private static TomeRegistry _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static TomeRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new TomeRegistry();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, ItemDefinition> _definitions =
            new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, GameObject> _prefabs =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        private TomeRegistry() { }

        /// <summary>
        /// Registers an item definition.
        /// The prefab will be created later during initialization.
        /// </summary>
        /// <param name="definition">The item definition to register</param>
        /// <returns>True if registered successfully</returns>
        public bool Register(ItemDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (!definition.Validate(out string error))
            {
                Plugin.Log?.LogError($"[Tome] Invalid item definition '{definition.PrefabName}': {error}");
                return false;
            }

            lock (_lock)
            {
                if (_definitions.ContainsKey(definition.PrefabName))
                {
                    Plugin.Log?.LogWarning($"[Tome] Item '{definition.PrefabName}' already registered, skipping");
                    return false;
                }

                _definitions[definition.PrefabName] = definition;
                Plugin.Log?.LogDebug($"[Tome] Registered item definition: {definition.PrefabName}");
                return true;
            }
        }

        /// <summary>
        /// Registers an item definition and its prefab together.
        /// </summary>
        /// <param name="definition">The item definition</param>
        /// <param name="prefab">The item prefab</param>
        /// <returns>True if registered successfully</returns>
        public bool Register(ItemDefinition definition, GameObject prefab)
        {
            if (!Register(definition))
                return false;

            if (prefab != null)
            {
                RegisterPrefab(definition.PrefabName, prefab);
            }

            return true;
        }

        /// <summary>
        /// Registers a prefab for a previously registered definition.
        /// </summary>
        internal void RegisterPrefab(string prefabName, GameObject prefab)
        {
            if (prefab == null)
                return;

            lock (_lock)
            {
                _prefabs[prefabName] = prefab;
                Plugin.Log?.LogDebug($"[Tome] Registered prefab: {prefabName}");
            }
        }

        /// <summary>
        /// Unregisters an item by prefab name.
        /// </summary>
        /// <param name="prefabName">The prefab name to remove</param>
        /// <returns>True if removed</returns>
        public bool Unregister(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return false;

            lock (_lock)
            {
                _prefabs.Remove(prefabName);
                return _definitions.Remove(prefabName);
            }
        }

        /// <summary>
        /// Gets an item definition by prefab name.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>The item definition, or null if not found</returns>
        public ItemDefinition GetDefinition(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;

            lock (_lock)
            {
                _definitions.TryGetValue(prefabName, out var def);
                return def;
            }
        }

        /// <summary>
        /// Gets a prefab by name.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>The prefab, or null if not found</returns>
        public GameObject GetPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;

            lock (_lock)
            {
                _prefabs.TryGetValue(prefabName, out var prefab);
                return prefab;
            }
        }

        /// <summary>
        /// Gets the ItemDrop.ItemData for a Tome item.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>The item data, or null if not found</returns>
        public ItemDrop.ItemData GetItemData(string prefabName)
        {
            var prefab = GetPrefab(prefabName);
            if (prefab == null)
                return null;

            if (prefab.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                return itemDrop.m_itemData;
            }

            return null;
        }

        /// <summary>
        /// Checks if an item is registered.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>True if registered</returns>
        public bool IsRegistered(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return false;

            lock (_lock)
            {
                return _definitions.ContainsKey(prefabName);
            }
        }

        /// <summary>
        /// Gets all registered item definitions.
        /// </summary>
        public IEnumerable<ItemDefinition> GetAll()
        {
            lock (_lock)
            {
                return _definitions.Values.ToList();
            }
        }

        /// <summary>
        /// Gets all registered prefab names.
        /// </summary>
        public IEnumerable<string> GetAllNames()
        {
            lock (_lock)
            {
                return _definitions.Keys.ToList();
            }
        }

        /// <summary>
        /// Gets all items in a specific category.
        /// </summary>
        /// <param name="category">The category to filter by</param>
        /// <returns>List of item definitions in that category</returns>
        public IEnumerable<ItemDefinition> GetByCategory(TomeCategory category)
        {
            lock (_lock)
            {
                return _definitions.Values.Where(d => d.Category == category).ToList();
            }
        }

        /// <summary>
        /// Gets all consumable items.
        /// </summary>
        public IEnumerable<ItemDefinition> GetConsumables()
        {
            lock (_lock)
            {
                return _definitions.Values.Where(d => d.Consumable).ToList();
            }
        }

        /// <summary>
        /// Gets all items with a linked Prime ability.
        /// </summary>
        public IEnumerable<ItemDefinition> GetItemsWithAbilities()
        {
            lock (_lock)
            {
                return _definitions.Values.Where(d => !string.IsNullOrEmpty(d.OnUseAbility)).ToList();
            }
        }

        /// <summary>
        /// Number of registered items.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _definitions.Count;
                }
            }
        }

        /// <summary>
        /// Clears all registered items. Use for testing only.
        /// </summary>
        internal void Clear()
        {
            lock (_lock)
            {
                _definitions.Clear();
                _prefabs.Clear();
            }
        }
    }
}
