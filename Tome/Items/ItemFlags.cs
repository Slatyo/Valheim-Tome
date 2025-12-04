using System;

namespace Tome.Items
{
    /// <summary>
    /// Flags that modify item behavior.
    /// </summary>
    [Flags]
    public enum ItemFlags
    {
        /// <summary>No special flags.</summary>
        None = 0,

        /// <summary>Item cannot be dropped.</summary>
        NoDrop = 1 << 0,

        /// <summary>Item cannot be destroyed/deleted.</summary>
        NoDestroy = 1 << 1,

        /// <summary>Item cannot be traded between players.</summary>
        NoTrade = 1 << 2,

        /// <summary>Item cannot be stored in containers.</summary>
        NoStore = 1 << 3,

        /// <summary>Item is soulbound to the player.</summary>
        Soulbound = NoDrop | NoTrade,

        /// <summary>Item is a quest item (can't drop, destroy, or trade).</summary>
        Quest = NoDrop | NoDestroy | NoTrade,

        /// <summary>Item is hidden from normal inventory view.</summary>
        Hidden = 1 << 4,

        /// <summary>Item stacks refresh duration when added.</summary>
        RefreshOnStack = 1 << 5,

        /// <summary>Item is automatically consumed when conditions are met.</summary>
        AutoConsume = 1 << 6
    }
}
