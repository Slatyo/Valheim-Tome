using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using Tome.Assets;
using Tome.Items;
using Tome.Registry;

namespace Tome
{
    /// <summary>
    /// Tome - Shared Item and Currency Registry for Valheim Mod Ecosystem.
    /// Provides a central database of items, currencies, and materials used across mods.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("com.slatyo.prime", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>Plugin GUID for BepInEx.</summary>
        public const string PluginGUID = "com.slatyo.tome";
        /// <summary>Plugin display name.</summary>
        public const string PluginName = "Tome";
        /// <summary>Plugin version.</summary>
        public const string PluginVersion = "1.0.0";

        /// <summary>
        /// Logger instance for Tome.
        /// </summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>
        /// Plugin instance.
        /// </summary>
        public static Plugin Instance { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");

            // Load asset bundles first
            LoadAssetBundles();

            // Load item definitions from embedded JSON
            LoadItemDefinitions();

            // Add localizations
            AddLocalizations();

            // Subscribe to Jotunn events
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;

            // Initialize Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded successfully");
            Log.LogInfo($"Registered {TomeRegistry.Instance.Count} item definitions");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            AssetBundleLoader.UnloadAll();
        }

        /// <summary>
        /// Loads asset bundles from embedded resources and external paths.
        /// </summary>
        private void LoadAssetBundles()
        {
            // Try to load the default Tome asset bundle (embedded)
            var defaultBundle = AssetBundleLoader.LoadEmbeddedBundle(AssetBundleLoader.DefaultBundleName);
            if (defaultBundle != null)
            {
                Log.LogInfo($"Loaded embedded asset bundle: {AssetBundleLoader.DefaultBundleName}");
            }

            // Scan for external bundles in config/Tome/AssetBundles/
            string externalBundlePath = Path.Combine(BepInEx.Paths.ConfigPath, "Tome", AssetBundleLoader.BundleDirectory);
            if (Directory.Exists(externalBundlePath))
            {
                foreach (var file in Directory.GetFiles(externalBundlePath))
                {
                    string bundleName = Path.GetFileNameWithoutExtension(file);
                    // Skip if extension is not empty and not .bundle
                    string ext = Path.GetExtension(file).ToLower();
                    if (!string.IsNullOrEmpty(ext) && ext != ".bundle")
                        continue;

                    var bundle = AssetBundleLoader.LoadExternalBundle(bundleName);
                    if (bundle != null)
                    {
                        Log.LogInfo($"Loaded external asset bundle: {bundleName}");
                    }
                }
            }

            // Also scan plugins/Tome/AssetBundles/
            string pluginBundlePath = Path.Combine(BepInEx.Paths.PluginPath, "Tome", AssetBundleLoader.BundleDirectory);
            if (Directory.Exists(pluginBundlePath))
            {
                foreach (var file in Directory.GetFiles(pluginBundlePath))
                {
                    string bundleName = Path.GetFileNameWithoutExtension(file);
                    string ext = Path.GetExtension(file).ToLower();
                    if (!string.IsNullOrEmpty(ext) && ext != ".bundle")
                        continue;

                    // Skip if already loaded
                    if (AssetBundleLoader.IsBundleLoaded(bundleName))
                        continue;

                    var bundle = AssetBundleLoader.LoadExternalBundle(bundleName);
                    if (bundle != null)
                    {
                        Log.LogInfo($"Loaded external asset bundle: {bundleName}");
                    }
                }
            }
        }

        /// <summary>
        /// Loads item definitions from embedded JSON resource.
        /// </summary>
        private void LoadItemDefinitions()
        {
            // Register built-in items from code first
            RegisterBuiltInItems();

            // Then load from embedded JSON
            int loaded = ItemLoader.LoadFromEmbeddedResource("Tome.Data.items.json");
            Log.LogDebug($"Loaded {loaded} items from embedded JSON");

            // Also check for external items.json in config folder
            string configPath = Path.Combine(BepInEx.Paths.ConfigPath, "Tome", "items.json");
            if (File.Exists(configPath))
            {
                int external = ItemLoader.LoadFromFile(configPath);
                Log.LogInfo($"Loaded {external} items from config file");
            }
        }

        /// <summary>
        /// Registers built-in items that don't come from JSON.
        /// These are items registered via code for more control.
        /// </summary>
        private void RegisterBuiltInItems()
        {
            // Items can be registered here via code if needed
            // Most items should come from items.json
        }

        /// <summary>
        /// Called when vanilla prefabs are available - creates actual item prefabs.
        /// </summary>
        private void OnVanillaPrefabsAvailable()
        {
            // Create all item prefabs from definitions
            int created = ItemCreator.CreateAllItems();
            Log.LogInfo($"Created {created} item prefabs");

            // Register abilities with Prime (if available)
            RegisterPrimeAbilities();

            // Unsubscribe - one-time event
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }

        /// <summary>
        /// Registers the Prime abilities used by Tome consumables.
        /// Only runs if Prime is available.
        /// </summary>
        private void RegisterPrimeAbilities()
        {
            if (!ConsumableHandler.IsPrimeAvailable)
            {
                Log.LogDebug("Prime not available, skipping ability registration");
                return;
            }

            try
            {
                // Register healing ability
                PrimeIntegration.RegisterAbility(new Prime.Abilities.AbilityDefinition("InstantHeal_Large")
                {
                    DisplayName = "$ability_instantheal_large",
                    Description = "$ability_instantheal_large_desc",
                    TargetType = Prime.Abilities.AbilityTargetType.Self,
                    Category = Prime.Abilities.AbilityCategory.Item,
                    BaseCooldown = 0f,
                    SelfEffects =
                    {
                        new Prime.Abilities.AbilityEffect("Health", Prime.Modifiers.ModifierType.Flat, 100f, 0f)
                    },
                    Tags = { "healing", "consumable", "tome" }
                });

                // Register stamina restore ability
                PrimeIntegration.RegisterAbility(new Prime.Abilities.AbilityDefinition("RestoreStamina_Large")
                {
                    DisplayName = "$ability_restorestamina_large",
                    Description = "$ability_restorestamina_large_desc",
                    TargetType = Prime.Abilities.AbilityTargetType.Self,
                    Category = Prime.Abilities.AbilityCategory.Item,
                    BaseCooldown = 0f,
                    SelfEffects =
                    {
                        new Prime.Abilities.AbilityEffect("Stamina", Prime.Modifiers.ModifierType.Flat, 100f, 0f)
                    },
                    Tags = { "restoration", "consumable", "tome" }
                });

                // Register eitr restore ability
                PrimeIntegration.RegisterAbility(new Prime.Abilities.AbilityDefinition("RestoreEitr_Large")
                {
                    DisplayName = "$ability_restoreeitr_large",
                    Description = "$ability_restoreeitr_large_desc",
                    TargetType = Prime.Abilities.AbilityTargetType.Self,
                    Category = Prime.Abilities.AbilityCategory.Item,
                    BaseCooldown = 0f,
                    SelfEffects =
                    {
                        new Prime.Abilities.AbilityEffect("Eitr", Prime.Modifiers.ModifierType.Flat, 50f, 0f)
                    },
                    Tags = { "restoration", "consumable", "tome", "magic" }
                });

                // Register speed buff ability
                PrimeIntegration.RegisterAbility(new Prime.Abilities.AbilityDefinition("SpeedBuff")
                {
                    DisplayName = "$ability_speedbuff",
                    Description = "$ability_speedbuff_desc",
                    TargetType = Prime.Abilities.AbilityTargetType.Self,
                    Category = Prime.Abilities.AbilityCategory.Item,
                    BaseCooldown = 0f,
                    SelfEffects =
                    {
                        new Prime.Abilities.AbilityEffect("MoveSpeed", Prime.Modifiers.ModifierType.Percent, 30f, 60f)
                    },
                    Tags = { "buff", "consumable", "tome", "movement" }
                });

                // Register strength buff ability
                PrimeIntegration.RegisterAbility(new Prime.Abilities.AbilityDefinition("StrengthBuff")
                {
                    DisplayName = "$ability_strengthbuff",
                    Description = "$ability_strengthbuff_desc",
                    TargetType = Prime.Abilities.AbilityTargetType.Self,
                    Category = Prime.Abilities.AbilityCategory.Item,
                    BaseCooldown = 0f,
                    SelfEffects =
                    {
                        new Prime.Abilities.AbilityEffect("Strength", Prime.Modifiers.ModifierType.Percent, 25f, 60f),
                        new Prime.Abilities.AbilityEffect("PhysicalDamage", Prime.Modifiers.ModifierType.Percent, 15f, 60f)
                    },
                    Tags = { "buff", "consumable", "tome", "offense" }
                });

                // Register armor buff ability
                PrimeIntegration.RegisterAbility(new Prime.Abilities.AbilityDefinition("ArmorBuff")
                {
                    DisplayName = "$ability_armorbuff",
                    Description = "$ability_armorbuff_desc",
                    TargetType = Prime.Abilities.AbilityTargetType.Self,
                    Category = Prime.Abilities.AbilityCategory.Item,
                    BaseCooldown = 0f,
                    SelfEffects =
                    {
                        new Prime.Abilities.AbilityEffect("Armor", Prime.Modifiers.ModifierType.Percent, 50f, 60f)
                    },
                    Tags = { "buff", "consumable", "tome", "defense" }
                });

                Log.LogInfo("Registered 6 Prime abilities for Tome consumables");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to register Prime abilities: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds localization entries for Tome items and abilities.
        /// </summary>
        private void AddLocalizations()
        {
            var customLoc = LocalizationManager.Instance.GetLocalization();

            // Add all item translations
            customLoc.AddTranslation("English", "item_rifttoken", "Rift Token");
            customLoc.AddTranslation("English", "item_rifttoken_desc", "A token of valor earned from conquering rift challenges. Used for enchanting and upgrades.");

            customLoc.AddTranslation("English", "item_arcaneessence", "Arcane Essence");
            customLoc.AddTranslation("English", "item_arcaneessence_desc", "Concentrated magical energy extracted from enchanted items.");

            customLoc.AddTranslation("English", "item_voidshard", "Void Shard");
            customLoc.AddTranslation("English", "item_voidshard_desc", "A fragment of pure void energy. Cannot pass through portals.");

            customLoc.AddTranslation("English", "item_soulfragment", "Soul Fragment");
            customLoc.AddTranslation("English", "item_soulfragment_desc", "A remnant of spiritual energy from defeated elite creatures.");

            customLoc.AddTranslation("English", "item_enchanteddust", "Enchanted Dust");
            customLoc.AddTranslation("English", "item_enchanteddust_desc", "Fine particles of magical residue.");

            customLoc.AddTranslation("English", "item_runeblank", "Rune Blank");
            customLoc.AddTranslation("English", "item_runeblank_desc", "An uncarved runestone ready for inscriptions.");

            customLoc.AddTranslation("English", "item_catalystcore", "Catalyst Core");
            customLoc.AddTranslation("English", "item_catalystcore_desc", "A powerful magical catalyst from bosses.");

            // Tokens/Keys
            customLoc.AddTranslation("English", "item_challengekey_t1", "Bronze Challenge Key");
            customLoc.AddTranslation("English", "item_challengekey_t1_desc", "Opens Bronze-tier rift portals.");

            customLoc.AddTranslation("English", "item_challengekey_t2", "Iron Challenge Key");
            customLoc.AddTranslation("English", "item_challengekey_t2_desc", "Opens Iron-tier rift portals.");

            customLoc.AddTranslation("English", "item_challengekey_t3", "Black Metal Challenge Key");
            customLoc.AddTranslation("English", "item_challengekey_t3_desc", "Opens Black Metal-tier rift portals.");

            // Consumables
            customLoc.AddTranslation("English", "item_healthelixir", "Health Elixir");
            customLoc.AddTranslation("English", "item_healthelixir_desc", "Instantly restores a large amount of health.");

            customLoc.AddTranslation("English", "item_staminaelixir", "Stamina Elixir");
            customLoc.AddTranslation("English", "item_staminaelixir_desc", "Instantly restores a large amount of stamina.");

            customLoc.AddTranslation("English", "item_eitrelixir", "Eitr Elixir");
            customLoc.AddTranslation("English", "item_eitrelixir_desc", "Instantly restores a large amount of eitr.");

            // Scrolls
            customLoc.AddTranslation("English", "item_swiftnessscroll", "Scroll of Swiftness");
            customLoc.AddTranslation("English", "item_swiftnessscroll_desc", "Grants increased movement speed.");

            customLoc.AddTranslation("English", "item_strengthscroll", "Scroll of Strength");
            customLoc.AddTranslation("English", "item_strengthscroll_desc", "Grants increased physical damage.");

            customLoc.AddTranslation("English", "item_protectionscroll", "Scroll of Protection");
            customLoc.AddTranslation("English", "item_protectionscroll_desc", "Grants increased armor.");

            // Abilities
            customLoc.AddTranslation("English", "ability_instantheal_large", "Instant Heal");
            customLoc.AddTranslation("English", "ability_instantheal_large_desc", "Instantly restores 100 health.");

            customLoc.AddTranslation("English", "ability_restorestamina_large", "Restore Stamina");
            customLoc.AddTranslation("English", "ability_restorestamina_large_desc", "Instantly restores 100 stamina.");

            customLoc.AddTranslation("English", "ability_restoreeitr_large", "Restore Eitr");
            customLoc.AddTranslation("English", "ability_restoreeitr_large_desc", "Instantly restores 50 eitr.");

            customLoc.AddTranslation("English", "ability_speedbuff", "Swiftness");
            customLoc.AddTranslation("English", "ability_speedbuff_desc", "Increases movement speed by 30% for 60 seconds.");

            customLoc.AddTranslation("English", "ability_strengthbuff", "Might");
            customLoc.AddTranslation("English", "ability_strengthbuff_desc", "Increases strength by 25% and physical damage by 15% for 60 seconds.");

            customLoc.AddTranslation("English", "ability_armorbuff", "Protection");
            customLoc.AddTranslation("English", "ability_armorbuff_desc", "Increases armor by 50% for 60 seconds.");

            // Messages
            customLoc.AddTranslation("English", "msg_cantdrop", "This item cannot be dropped");

            // === GERMAN TRANSLATIONS ===
            // Currencies & Materials
            customLoc.AddTranslation("German", "item_rifttoken", "Riss-Marke");
            customLoc.AddTranslation("German", "item_rifttoken_desc", "Eine Tapferkeitsmarke aus Riss-Herausforderungen. Wird für Verzauberungen und Verbesserungen verwendet.");

            customLoc.AddTranslation("German", "item_arcaneessence", "Arkane Essenz");
            customLoc.AddTranslation("German", "item_arcaneessence_desc", "Konzentrierte magische Energie aus verzauberten Gegenständen.");

            customLoc.AddTranslation("German", "item_voidshard", "Leerensplitter");
            customLoc.AddTranslation("German", "item_voidshard_desc", "Ein Fragment reiner Leerenenergie. Kann nicht durch Portale transportiert werden.");

            customLoc.AddTranslation("German", "item_soulfragment", "Seelenfragment");
            customLoc.AddTranslation("German", "item_soulfragment_desc", "Ein Überrest spiritueller Energie von besiegten Elitekreaturen.");

            customLoc.AddTranslation("German", "item_enchanteddust", "Verzauberter Staub");
            customLoc.AddTranslation("German", "item_enchanteddust_desc", "Feine Partikel magischer Rückstände.");

            customLoc.AddTranslation("German", "item_runeblank", "Runenrohling");
            customLoc.AddTranslation("German", "item_runeblank_desc", "Ein ungeschnitzter Runenstein bereit für Inschriften.");

            customLoc.AddTranslation("German", "item_catalystcore", "Katalysatorkern");
            customLoc.AddTranslation("German", "item_catalystcore_desc", "Ein mächtiger magischer Katalysator von Bossen.");

            // Tokens/Keys
            customLoc.AddTranslation("German", "item_challengekey_t1", "Bronze-Herausforderungsschlüssel");
            customLoc.AddTranslation("German", "item_challengekey_t1_desc", "Öffnet Bronze-Stufen-Rissportale.");

            customLoc.AddTranslation("German", "item_challengekey_t2", "Eisen-Herausforderungsschlüssel");
            customLoc.AddTranslation("German", "item_challengekey_t2_desc", "Öffnet Eisen-Stufen-Rissportale.");

            customLoc.AddTranslation("German", "item_challengekey_t3", "Schwarzmetall-Herausforderungsschlüssel");
            customLoc.AddTranslation("German", "item_challengekey_t3_desc", "Öffnet Schwarzmetall-Stufen-Rissportale.");

            // Consumables
            customLoc.AddTranslation("German", "item_healthelixir", "Lebenselixier");
            customLoc.AddTranslation("German", "item_healthelixir_desc", "Stellt sofort eine große Menge Gesundheit wieder her.");

            customLoc.AddTranslation("German", "item_staminaelixir", "Ausdauerelixier");
            customLoc.AddTranslation("German", "item_staminaelixir_desc", "Stellt sofort eine große Menge Ausdauer wieder her.");

            customLoc.AddTranslation("German", "item_eitrelixir", "Eitr-Elixier");
            customLoc.AddTranslation("German", "item_eitrelixir_desc", "Stellt sofort eine große Menge Eitr wieder her.");

            // Scrolls
            customLoc.AddTranslation("German", "item_swiftnessscroll", "Schriftrolle der Schnelligkeit");
            customLoc.AddTranslation("German", "item_swiftnessscroll_desc", "Gewährt erhöhte Bewegungsgeschwindigkeit.");

            customLoc.AddTranslation("German", "item_strengthscroll", "Schriftrolle der Stärke");
            customLoc.AddTranslation("German", "item_strengthscroll_desc", "Gewährt erhöhten physischen Schaden.");

            customLoc.AddTranslation("German", "item_protectionscroll", "Schriftrolle des Schutzes");
            customLoc.AddTranslation("German", "item_protectionscroll_desc", "Gewährt erhöhte Rüstung.");

            // Abilities
            customLoc.AddTranslation("German", "ability_instantheal_large", "Sofortige Heilung");
            customLoc.AddTranslation("German", "ability_instantheal_large_desc", "Stellt sofort 100 Gesundheit wieder her.");

            customLoc.AddTranslation("German", "ability_restorestamina_large", "Ausdauer wiederherstellen");
            customLoc.AddTranslation("German", "ability_restorestamina_large_desc", "Stellt sofort 100 Ausdauer wieder her.");

            customLoc.AddTranslation("German", "ability_restoreeitr_large", "Eitr wiederherstellen");
            customLoc.AddTranslation("German", "ability_restoreeitr_large_desc", "Stellt sofort 50 Eitr wieder her.");

            customLoc.AddTranslation("German", "ability_speedbuff", "Behändigkeit");
            customLoc.AddTranslation("German", "ability_speedbuff_desc", "Erhöht die Bewegungsgeschwindigkeit um 30% für 60 Sekunden.");

            customLoc.AddTranslation("German", "ability_strengthbuff", "Macht");
            customLoc.AddTranslation("German", "ability_strengthbuff_desc", "Erhöht Stärke um 25% und physischen Schaden um 15% für 60 Sekunden.");

            customLoc.AddTranslation("German", "ability_armorbuff", "Schutz");
            customLoc.AddTranslation("German", "ability_armorbuff_desc", "Erhöht Rüstung um 50% für 60 Sekunden.");

            // Messages
            customLoc.AddTranslation("German", "msg_cantdrop", "Dieser Gegenstand kann nicht abgelegt werden");
        }

        /// <summary>
        /// Helper method to check if running on server.
        /// </summary>
        public static bool IsServer() => ZNet.instance != null && ZNet.instance.IsServer();

        /// <summary>
        /// Helper method to check if running on client.
        /// </summary>
        public static bool IsClient() => ZNet.instance != null && !ZNet.instance.IsServer();
    }
}
