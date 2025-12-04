<p align="center">
  <img src="Tome/Package/icon.png" alt="Tome" width="128">
</p>

<h1 align="center">Valheim-Tome</h1>

<p align="center">
  <a href="https://github.com/Slatyo/Valheim-Tome/releases"><img src="https://img.shields.io/github/v/release/Slatyo/Valheim-Tome?style=flat-square" alt="GitHub release"></a>
  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square" alt="License: MIT"></a>
</p>

<p align="center">
  Shared item and currency registry for the Valheim mod ecosystem.<br>
  Currencies, materials, consumables - one registry for all mods!
</p>

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

Add Tome as a dependency to access the shared item registry:

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
    // Success - player had enough tokens
}

// Award currency
TomeAPI.AwardCurrency(player, "RiftToken", 5);

// Query items by category
var currencies = TomeRegistry.Instance.GetItemsByCategory(TomeCategory.Currency);
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Acknowledgments

- Built using [JotunnModStub](https://github.com/Valheim-Modding/JotunnModStub) template
- Powered by [Jotunn](https://valheim-modding.github.io/Jotunn/) - the Valheim Library
- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity game patcher and plugin framework

## License

[MIT](LICENSE) - Slatyo
