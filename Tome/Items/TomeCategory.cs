namespace Tome.Items
{
    /// <summary>
    /// Standard item categories for Tome items.
    /// Used for sorting, filtering, and UI organization.
    /// </summary>
    public enum TomeCategory
    {
        /// <summary>Currency items like tokens and coins.</summary>
        Currency,

        /// <summary>Crafting materials used in recipes.</summary>
        CraftingMaterial,

        /// <summary>Consumable items (potions, scrolls, etc.).</summary>
        Consumable,

        /// <summary>Rune items for enchanting.</summary>
        Rune,

        /// <summary>Scroll items with temporary effects.</summary>
        Scroll,

        /// <summary>Token items used for special vendors or access.</summary>
        Token,

        /// <summary>Trophy items from defeated enemies.</summary>
        Trophy,

        /// <summary>Quest-related items.</summary>
        QuestItem,

        /// <summary>Miscellaneous uncategorized items.</summary>
        Misc
    }
}
