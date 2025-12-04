using HarmonyLib;
using Tome.Items;

namespace Tome.Patches
{
    /// <summary>
    /// Harmony patches for intercepting item consumption.
    /// </summary>
    [HarmonyPatch]
    public static class ConsumptionPatches
    {
        /// <summary>
        /// Patch to trigger Tome abilities when items are consumed.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        [HarmonyPostfix]
        public static void Player_ConsumeItem_Postfix(Player __instance, ItemDrop.ItemData item, bool __result)
        {
            // Only process if item was actually consumed
            if (!__result || __instance == null || item == null)
                return;

            // Check if this is a Tome consumable with an ability
            ConsumableHandler.OnItemConsumed(__instance, item);
        }

        /// <summary>
        /// Patch to prevent dropping of items with NoDrop flag.
        /// </summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
        [HarmonyPrefix]
        public static bool Humanoid_DropItem_Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item)
        {
            if (item == null)
                return true;

            string prefabName = item.m_dropPrefab?.name;
            if (string.IsNullOrEmpty(prefabName))
                return true;

            var def = Registry.TomeRegistry.Instance.GetDefinition(prefabName);
            if (def != null && def.HasFlag(ItemFlags.NoDrop))
            {
                // Show message to player
                if (__instance is Player player)
                {
                    player.Message(MessageHud.MessageType.Center, "$msg_cantdrop");
                }
                return false; // Prevent drop
            }

            return true; // Allow drop
        }

        /// <summary>
        /// Patch to prevent destroying items with NoDestroy flag.
        /// </summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(ItemDrop.ItemData))]
        [HarmonyPrefix]
        public static bool Inventory_RemoveItem_Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (item == null)
                return true;

            string prefabName = item.m_dropPrefab?.name;
            if (string.IsNullOrEmpty(prefabName))
                return true;

            var def = Registry.TomeRegistry.Instance.GetDefinition(prefabName);
            if (def != null && def.HasFlag(ItemFlags.NoDestroy))
            {
                __result = false;
                return false; // Prevent removal/destruction
            }

            return true;
        }
    }
}
