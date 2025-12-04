using System;
using Tome.Registry;

namespace Tome.Items
{
    /// <summary>
    /// Handles consumable item effects, including triggering Prime abilities.
    /// </summary>
    public static class ConsumableHandler
    {
        private static bool _primeAvailable;
        private static bool _primeChecked;

        /// <summary>
        /// Checks if Prime mod is available.
        /// </summary>
        public static bool IsPrimeAvailable
        {
            get
            {
                if (!_primeChecked)
                {
                    CheckPrimeAvailability();
                }
                return _primeAvailable;
            }
        }

        private static void CheckPrimeAvailability()
        {
            _primeChecked = true;
            try
            {
                // Check if Prime assembly is loaded
                var primeType = Type.GetType("Prime.PrimeAPI, Prime");
                _primeAvailable = primeType != null;

                if (_primeAvailable)
                {
                    Plugin.Log?.LogInfo("[Tome] Prime detected - consumable abilities enabled");
                }
                else
                {
                    Plugin.Log?.LogInfo("[Tome] Prime not detected - consumable abilities disabled");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[Tome] Prime check failed: {ex.Message}");
                _primeAvailable = false;
            }
        }

        /// <summary>
        /// Called when a player consumes an item.
        /// Triggers linked Prime ability if available.
        /// </summary>
        /// <param name="player">The player consuming</param>
        /// <param name="item">The item being consumed</param>
        /// <returns>True if a Tome ability was triggered</returns>
        public static bool OnItemConsumed(Player player, ItemDrop.ItemData item)
        {
            if (player == null || item == null)
                return false;

            // Get the prefab name
            string prefabName = item.m_dropPrefab?.name;
            if (string.IsNullOrEmpty(prefabName))
                return false;

            // Check if this is a Tome item with an ability
            var def = TomeRegistry.Instance.GetDefinition(prefabName);
            if (def == null || string.IsNullOrEmpty(def.OnUseAbility))
                return false;

            // Try to trigger the Prime ability
            return TriggerPrimeAbility(player, def.OnUseAbility);
        }

        /// <summary>
        /// Triggers a Prime ability for a player.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="abilityId">The Prime ability ID</param>
        /// <returns>True if triggered successfully</returns>
        public static bool TriggerPrimeAbility(Player player, string abilityId)
        {
            if (!IsPrimeAvailable)
            {
                Plugin.Log?.LogWarning($"[Tome] Cannot trigger ability '{abilityId}' - Prime not available");
                return false;
            }

            try
            {
                return PrimeIntegration.UseAbility(player, abilityId);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Tome] Failed to trigger ability '{abilityId}': {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Prime integration layer - isolated to handle missing reference gracefully.
    /// </summary>
    internal static class PrimeIntegration
    {
        /// <summary>
        /// Uses a Prime ability on a player.
        /// </summary>
        public static bool UseAbility(Player player, string abilityId)
        {
            // Direct call to Prime API
            // If Prime is not loaded, this will throw a TypeLoadException caught by ConsumableHandler
            var character = player as Character;
            if (character == null)
                return false;

            // First grant the ability temporarily if not already granted
            if (!Prime.PrimeAPI.HasAbility(character, abilityId))
            {
                Prime.PrimeAPI.GrantAbility(character, abilityId);
            }

            // Use the ability
            bool result = Prime.PrimeAPI.UseAbility(character, abilityId, null);

            Plugin.Log?.LogDebug($"[Tome] Prime ability '{abilityId}' triggered: {result}");
            return result;
        }

        /// <summary>
        /// Checks if an ability is registered in Prime.
        /// </summary>
        public static bool IsAbilityRegistered(string abilityId)
        {
            return Prime.PrimeAPI.GetAbility(abilityId) != null;
        }

        /// <summary>
        /// Registers an ability with Prime.
        /// </summary>
        public static bool RegisterAbility(Prime.Abilities.AbilityDefinition ability)
        {
            return Prime.PrimeAPI.RegisterAbility(ability);
        }
    }
}
