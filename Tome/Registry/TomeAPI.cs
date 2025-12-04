using System;
using System.Collections.Generic;
using System.Linq;
using Tome.Items;
using UnityEngine;

namespace Tome.Registry
{
    /// <summary>
    /// Public API for other mods to interact with Tome.
    /// Provides convenience methods for currency management and item queries.
    /// </summary>
    public static class TomeAPI
    {
        /// <summary>
        /// Access to the item registry.
        /// </summary>
        public static TomeRegistry Registry => TomeRegistry.Instance;

        // ==================== ITEM QUERIES ====================

        /// <summary>
        /// Gets a Tome item prefab by name.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>The prefab, or null if not found</returns>
        public static GameObject GetPrefab(string prefabName)
        {
            return Registry.GetPrefab(prefabName);
        }

        /// <summary>
        /// Gets a Tome item definition by name.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>The definition, or null if not found</returns>
        public static ItemDefinition GetDefinition(string prefabName)
        {
            return Registry.GetDefinition(prefabName);
        }

        /// <summary>
        /// Checks if an item is a Tome shared item.
        /// </summary>
        /// <param name="prefabName">The prefab name to check</param>
        /// <returns>True if this is a Tome item</returns>
        public static bool IsSharedItem(string prefabName)
        {
            return Registry.IsRegistered(prefabName);
        }

        /// <summary>
        /// Gets the category of a Tome item.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>The category, or null if not a Tome item</returns>
        public static TomeCategory? GetCategory(string prefabName)
        {
            var def = Registry.GetDefinition(prefabName);
            return def?.Category;
        }

        /// <summary>
        /// Gets all items in a category.
        /// </summary>
        /// <param name="category">The category</param>
        /// <returns>List of prefab names</returns>
        public static IEnumerable<string> GetItemsByCategory(TomeCategory category)
        {
            return Registry.GetByCategory(category).Select(d => d.PrefabName);
        }

        // ==================== ABILITY QUERIES ====================

        /// <summary>
        /// Gets the Prime ability ID linked to an item.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>The ability ID, or null if none</returns>
        public static string GetItemAbility(string prefabName)
        {
            var def = Registry.GetDefinition(prefabName);
            return def?.OnUseAbility;
        }

        /// <summary>
        /// Checks if an item has a linked ability.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <returns>True if the item has an ability</returns>
        public static bool HasAbility(string prefabName)
        {
            var def = Registry.GetDefinition(prefabName);
            return !string.IsNullOrEmpty(def?.OnUseAbility);
        }

        // ==================== CURRENCY MANAGEMENT ====================

        /// <summary>
        /// Gets the amount of a currency item in a player's inventory.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="currencyPrefab">The currency prefab name</param>
        /// <returns>Total count of the currency</returns>
        public static int GetPlayerCurrency(Player player, string currencyPrefab)
        {
            if (player == null || string.IsNullOrEmpty(currencyPrefab))
                return 0;

            var inventory = player.GetInventory();
            if (inventory == null)
                return 0;

            return inventory.CountItems(currencyPrefab);
        }

        /// <summary>
        /// Consumes a specific amount of currency from a player's inventory.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="currencyPrefab">The currency prefab name</param>
        /// <param name="amount">Amount to consume</param>
        /// <returns>True if consumed successfully</returns>
        public static bool ConsumeCurrency(Player player, string currencyPrefab, int amount)
        {
            if (player == null || string.IsNullOrEmpty(currencyPrefab) || amount <= 0)
                return false;

            var inventory = player.GetInventory();
            if (inventory == null)
                return false;

            int current = inventory.CountItems(currencyPrefab);
            if (current < amount)
                return false;

            inventory.RemoveItem(currencyPrefab, amount);
            return true;
        }

        /// <summary>
        /// Awards currency to a player's inventory.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="currencyPrefab">The currency prefab name</param>
        /// <param name="amount">Amount to award</param>
        /// <returns>True if awarded successfully</returns>
        public static bool AwardCurrency(Player player, string currencyPrefab, int amount)
        {
            if (player == null || string.IsNullOrEmpty(currencyPrefab) || amount <= 0)
                return false;

            var prefab = Registry.GetPrefab(currencyPrefab);
            if (prefab == null)
            {
                Plugin.Log?.LogWarning($"[Tome] Cannot award currency '{currencyPrefab}' - prefab not found");
                return false;
            }

            var inventory = player.GetInventory();
            if (inventory == null)
                return false;

            return inventory.AddItem(prefab, amount);
        }

        /// <summary>
        /// Checks if a player can afford a currency cost.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="currencyPrefab">The currency prefab name</param>
        /// <param name="amount">Amount required</param>
        /// <returns>True if the player has enough</returns>
        public static bool CanAfford(Player player, string currencyPrefab, int amount)
        {
            return GetPlayerCurrency(player, currencyPrefab) >= amount;
        }

        /// <summary>
        /// Checks if a player can afford multiple currency costs.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="costs">Dictionary of prefab name to amount</param>
        /// <returns>True if the player has all currencies</returns>
        public static bool CanAfford(Player player, Dictionary<string, int> costs)
        {
            if (player == null || costs == null)
                return false;

            foreach (var kvp in costs)
            {
                if (!CanAfford(player, kvp.Key, kvp.Value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Consumes multiple currencies from a player.
        /// Only succeeds if ALL currencies are available.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="costs">Dictionary of prefab name to amount</param>
        /// <returns>True if all currencies were consumed</returns>
        public static bool ConsumeCurrencies(Player player, Dictionary<string, int> costs)
        {
            if (!CanAfford(player, costs))
                return false;

            foreach (var kvp in costs)
            {
                if (!ConsumeCurrency(player, kvp.Key, kvp.Value))
                {
                    // This shouldn't happen if CanAfford passed, but log it
                    Plugin.Log?.LogError($"[Tome] Failed to consume '{kvp.Key}' x{kvp.Value} after CanAfford check passed");
                    return false;
                }
            }

            return true;
        }

        // ==================== ITEM SPAWNING ====================

        /// <summary>
        /// Spawns a Tome item at a world position.
        /// </summary>
        /// <param name="prefabName">The prefab name</param>
        /// <param name="position">World position</param>
        /// <param name="amount">Stack amount</param>
        /// <returns>The spawned GameObject, or null on failure</returns>
        public static GameObject SpawnItem(string prefabName, Vector3 position, int amount = 1)
        {
            var prefab = Registry.GetPrefab(prefabName);
            if (prefab == null)
            {
                Plugin.Log?.LogWarning($"[Tome] Cannot spawn item '{prefabName}' - prefab not found");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);

            if (instance.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                itemDrop.m_itemData.m_stack = Math.Max(1, amount);
            }

            return instance;
        }

        /// <summary>
        /// Drops a Tome item from a player's position.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="prefabName">The prefab name</param>
        /// <param name="amount">Stack amount</param>
        /// <returns>The spawned GameObject, or null on failure</returns>
        public static GameObject DropItem(Player player, string prefabName, int amount = 1)
        {
            if (player == null)
                return null;

            Vector3 dropPos = player.transform.position + player.transform.forward * 1.5f + Vector3.up * 0.5f;
            return SpawnItem(prefabName, dropPos, amount);
        }

        // ==================== INVENTORY HELPERS ====================

        /// <summary>
        /// Finds all Tome items in a player's inventory.
        /// </summary>
        /// <param name="player">The player</param>
        /// <returns>Dictionary of prefab name to total count</returns>
        public static Dictionary<string, int> GetAllTomeItems(Player player)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (player == null)
                return result;

            var inventory = player.GetInventory();
            if (inventory == null)
                return result;

            foreach (var item in inventory.GetAllItems())
            {
                string prefabName = item.m_dropPrefab?.name;
                if (string.IsNullOrEmpty(prefabName))
                    continue;

                if (Registry.IsRegistered(prefabName))
                {
                    if (result.ContainsKey(prefabName))
                        result[prefabName] += item.m_stack;
                    else
                        result[prefabName] = item.m_stack;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all Tome items of a specific category in a player's inventory.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="category">The category to filter</param>
        /// <returns>Dictionary of prefab name to total count</returns>
        public static Dictionary<string, int> GetTomeItemsByCategory(Player player, TomeCategory category)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (player == null)
                return result;

            var inventory = player.GetInventory();
            if (inventory == null)
                return result;

            foreach (var item in inventory.GetAllItems())
            {
                string prefabName = item.m_dropPrefab?.name;
                if (string.IsNullOrEmpty(prefabName))
                    continue;

                var def = Registry.GetDefinition(prefabName);
                if (def != null && def.Category == category)
                {
                    if (result.ContainsKey(prefabName))
                        result[prefabName] += item.m_stack;
                    else
                        result[prefabName] = item.m_stack;
                }
            }

            return result;
        }
    }
}
