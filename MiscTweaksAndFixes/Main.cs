using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;

using Microsoftenator.Wotr.Common;
using Microsoftenator.Wotr.Common.Blueprints;
using Microsoftenator.Wotr.Common.Localization;
using Microsoftenator.Wotr.Common.ModTemplate;
using Microsoftenator.Wotr.Common.Util;

using UnityEngine;

using UnityModManagerNet;
using ModMenu;
using ModMenu.Settings;
using Kingmaker.UI.SettingsUI;

namespace MiscTweaksAndFixes
{
    public class MiscTweaksMod : ModBase
    {
        //    public class ModSettings : UnityModManager.ModSettings, IDrawable
        //    {
        //        [Draw("Natural weapon damage stacking")] public bool NaturalWeaponDamageStacking = true;

        //        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);

        //        public void OnChange()
        //        {
        //            Main.ApplySettings(this);
        //        }
        //    }

        //    public ModSettings Settings { get; internal set; }

        //    public void ApplySettings(ModSettings settings)
        //    {
        //        Settings = settings;

        //        NaturalWeaponStacking.NaturalAttackStackingPatch(settings.NaturalWeaponDamageStacking);
        //    }

        //    public MiscTweaksMod()
        //    {
        //        Settings = new();

        //        this.OnGUI = Settings.Draw;
        //        this.OnSaveGUI = Settings.Save;
        //    }
    }

    static class Main
    {
        internal static MiscTweaksMod Mod = new();

        //internal static readonly int SharedModVersion = 0;

        //internal static Func<IEnumerable<BlueprintInfo>, bool> AddSharedBlueprints { get; private set; } = _ => false;

        internal static Microsoftenator.Wotr.Common.ModTemplate.Logger Log => Mod.Log;

        internal static bool Enabled { get; private set; } = false;

        //internal static void ApplySettings(MiscTweaksMod.ModSettings settings)
        //{
        //    Mod.ApplySettings(settings);
        //}

        private static LocalizedString CreateString(string key, string str)
        {
            Localization.Default.Add(key, str);

            return Localization.Default.Get(key) ?? throw new NullReferenceException();
        }

        private static string SettingsRootKey => $"{Main.Mod.ModEntry.Info.Id}".ToLower();

        private static Toggle CreateSettingToggle(string name, string description, bool defaultValue = true, string? longDescription = null)
        {
            var nameKey = $"{SettingsRootKey}.{name}".ToLower();

            Main.Log.Debug($"New toggle key: \"{nameKey}\"");

            var toggle = Toggle.New(nameKey, defaultValue, CreateString($"{nameKey}.Toggle.Description", description));

            if(longDescription is not null)
                toggle = toggle.WithLongDescription(CreateString($"{nameKey}.Toggle.LongDescription", longDescription));

            return toggle;
        }

        internal static void SettingsInit()
        {
            var primalistToggle = 
                CreateSettingToggle(
                    $"{nameof(Primalist.PrimalistBloodlineFixes)}",
                    "Primalist bloodline selection fix",
                    longDescription:
                        "Primalist bloodline selections are now per-bloodline and should function correctly when "
                        + "combined with Dragon Disciple and/or Second Bloodline (still two rage powers per 4 "
                        + "levels, but you can choose which bloodline's power to trade)\n"
                        + "Requires restart.")
                .OnValueChanged(newValue => Primalist.PrimalistBloodlineFixes.Enabled = newValue);

            var bookOfDreamsToggle = 
                CreateSettingToggle(
                    $"{nameof(BookOfDreams.BookOfDreamsFix)}",
                    "Book of Dreams upgrade fix",
                    defaultValue: false,
                    longDescription:
                        "The Book of Dreams item is supposed to upgrade at certain points in the story, "
                        + "but this has never reliably worked (at least in my experience).\n"
                        + "Enabling this forces the upgrade script to run on every Etude update.")
                .OnValueChanged(newValue => BookOfDreams.BookOfDreamsFix.Enabled = newValue);

            var naturalWeaponStacking =
                CreateSettingToggle(
                    $"{nameof(NaturalWeaponStacking.NaturalWeaponStacking)}",
                    "Natural weapon stacking",
                    longDescription:
                        "Previously, if you got multiple natural attacks of the same type from different "
                        + "features/buffs/etc. you would get extra attacks per round. This was 'fixed' by Owlcat at "
                        + "some point so now extra natural attacks give no benefit to PCs.\n"
                        + "With this enabled, vanilla behaviour is replaced with an approximation of the tabletop rules:\n"
                        + "Addtional natural attacks of the same kind gives a stacking increase to the effective size "
                        + "of the 'weapon' (eg. 2 pairs of Medium claw attacks effectively grant 1 pair of Large claw "
                        + "attacks instead).\n"
                        + "You get all 'enchantment' effects (eg. fire damage/DR penetration) but multiple enchants "
                        + "of the same type do not stack.")
                .OnValueChanged(newValue => NaturalWeaponStacking.NaturalWeaponStacking.Enabled = newValue);

            var reformedFiendDRToggle =
                CreateSettingToggle(
                    $"{nameof(ReformedFiend.ReformedFiendDamageReductionGood)}",
                    "Reformed Fiend DR/good",
                    defaultValue: false,
                    longDescription:
                        "Changes the damage reduction for the Reformed Fiend Bloodrage archetype from DR/evil to "
                        + "DR/good.\n"
                        + "Requires restart.")
                .OnValueChanged(newValue => ReformedFiend.ReformedFiendDamageReductionGood.Enabled = newValue);

            var strengthBlessingMajorFixToggle =
                CreateSettingToggle(
                    $"{nameof(StrengthBlessingMajor.StrengthBlessingMajorBuff)}",
                    "Major Strength Blessing armor speed fix",
                    longDescription:
                        "Warpriest's Major Blessing for Strength domain now applies to heavy armor in addition to " +
                        "medium armor.\n"
                        + "Requires restart.")
                .OnValueChanged(newValue => StrengthBlessingMajor.StrengthBlessingMajorBuff.Enabled = newValue);

            var settings =
                SettingsBuilder.New(SettingsRootKey,
                    CreateString($"{nameof(MiscTweaksAndFixes)}.Title", "Miscellaneous Tweaks and Fixes"))
                .AddToggle(primalistToggle)
                .AddToggle(bookOfDreamsToggle)
                .AddToggle(naturalWeaponStacking)
                .AddToggle(reformedFiendDRToggle)
                .AddToggle(strengthBlessingMajorFixToggle);
            
            ModMenu.ModMenu.AddSettings(settings);
        }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            Log.Debug($"{nameof(Main)}.{nameof(Load)}");

            var harmony = new Harmony(modEntry.Info.Id);

            Mod.OnLoad(modEntry, harmony);

            //Mod.Settings = UnityModManager.ModSettings.Load<MiscTweaksMod.ModSettings>(modEntry);

            harmony.PatchAll();

            //SharedMods.Register(modEntry.Info.Id, SharedModVersion);
            //AddSharedBlueprints = blueprints => SharedMods.AddBlueprints(modEntry.Info.Id, SharedModVersion, blueprints);

            return true;
        }
    }

    internal static class Localization
    {
        private static readonly Lazy<LocalizedStringsPack> defaultStringsLazy = new(() => new(LocalizationManager.CurrentLocale));
        public static LocalizedStringsPack Default => defaultStringsLazy.Value;
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    [HarmonyAfter("TabletopTweaks-Core", "TabletopTweaks-Base")]
    internal class BlueprintsCache_Init_Patch
    {
        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var addMethod = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.GetType().GetMethod("Add");
        //    var setMethod = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.GetType().GetMethod("set_Item");

        //    return instructions.Select(i =>
        //    {
        //        if (i.opcode == OpCodes.Callvirt && i.Calls(addMethod))
        //            i.operand = setMethod;
        //        return i;
        //    });
        //}

        private static bool completed;

        static void Postfix()
        {
            if (completed)
            {
                Main.Log.Warning($"Duplicate call to {nameof(BlueprintsCache_Init_Patch)}.{nameof(Postfix)}");
                return;
            }

            completed = true;

            Main.Log.Debug($"{nameof(BlueprintsCache_Init_Patch)}.{nameof(Postfix)}");

            Main.SettingsInit();

            ReformedFiend.ReformedFiendDamageReductionGood.PatchDamageReduction();
            BookOfDreams.BookOfDreamsFix.BookOfDreamsUpgradeFix();
            StrengthBlessingMajor.StrengthBlessingMajorBuff.ArmorSpeedFix();
            Primalist.PrimalistBloodlineFixes.PatchPrimalistProgression();

            Localization.Default.LoadAll();
        }
    }
}
