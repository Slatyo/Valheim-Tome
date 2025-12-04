# Tome

Shared item and currency registry for the Valheim mod ecosystem.

## Features

- **Shared Currencies** - Rift Tokens, Arcane Essence, Void Shards, Soul Fragments
- **Crafting Materials** - Enchanted Dust, Rune Blanks, Catalyst Cores
- **Challenge Keys** - Bronze, Iron, and Black Metal tier keys for rift portals
- **Consumables** - Health/Stamina/Eitr Elixirs, buff scrolls (with Prime integration)
- **Unified API** - `TomeRegistry` and `TomeAPI` for cross-mod item management
- **Server-Synced** - All items synced between server and clients

## Installation

### Thunderstore (Recommended)
Install via [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).

### Manual
1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Install [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)
3. Place `Tome.dll` in `BepInEx/plugins/`

**Note:** All players and the server must have the mod installed.

## For Mod Developers

### Dependency Setup
```csharp
[BepInDependency("com.tome.valheim")]
```

### Using the API
```csharp
using Tome;
using Tome.Registry;

// Get item prefab
var token = TomeRegistry.Instance.GetPrefab("RiftToken");

// Check player currency
int tokens = TomeAPI.GetPlayerCurrency(player, "RiftToken");

// Consume currency
if (TomeAPI.ConsumeCurrency(player, "RiftToken", 10))
{
    // Success
}

// Award currency
TomeAPI.AwardCurrency(player, "RiftToken", 5);

// Query items by category
var currencies = TomeRegistry.Instance.GetItemsByCategory(TomeCategory.Currency);
```

## Item Categories

| Category | Description |
|----------|-------------|
| Currency | Stackable tokens and currencies |
| CraftingMaterial | Materials for crafting systems |
| Consumable | Items that trigger effects when used |
| Token | Keys and access tokens |
| Rune | Rune items for enchanting |
| Scroll | Buff scrolls |
| Trophy | Trophy items |
| QuestItem | Quest-related items |

## Prime Integration (Optional)

If [Prime](https://github.com/Slatyo/Valheim-Prime) is installed, consumable items can trigger Prime abilities:

- Health Elixir -> InstantHeal_Large (100 HP)
- Stamina Elixir -> RestoreStamina_Large (100 Stamina)
- Eitr Elixir -> RestoreEitr_Large (50 Eitr)
- Scroll of Swiftness -> SpeedBuff (+30% speed, 60s)
- Scroll of Strength -> StrengthBuff (+25% strength, 60s)
- Scroll of Protection -> ArmorBuff (+50% armor, 60s)

## Configuration

Custom items can be added via `BepInEx/config/Tome/items.json`:

```json
{
  "Items": [
    {
      "PrefabName": "MyCustomToken",
      "DisplayName": "$item_mycustomtoken",
      "Description": "$item_mycustomtoken_desc",
      "Category": "Currency",
      "MaxStack": 999,
      "Weight": 0.1,
      "Tradeable": true
    }
  ]
}
```

## License

MIT
