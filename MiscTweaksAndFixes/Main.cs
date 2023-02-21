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

using MiscTweaksAndFixes.Bloodrager;
using MiscTweaksAndFixes.Bloodrager.Primalist;

namespace System.Runtime.CompilerServices
{
    [DebuggerNonUserCode]
    internal static class IsExternalInit { }
}

namespace MiscTweaksAndFixes
{
    internal partial class MiscTweaksMod : ModBase
    {
        internal class Logger : Microsoftenator.Wotr.Common.ModTemplate.Logger
        {
            internal Logger(UnityModManager.ModEntry modEntry, Microsoftenator.Wotr.Common.ModTemplate.Logger other)
                : base(modEntry.Logger, other) { }

            public override void Debug(string message)
            {
                if(Main.Mod.Settings.DebugLogging)
                    base.Debug(message);
            }
        }

        internal readonly ModSettings Settings;

        internal MiscTweaksMod()
        {
            Settings = new();
        }

        public override bool OnLoad(UnityModManager.ModEntry modEntry, Harmony? harmony = null,
            bool harmonyPatch = false)
        {
            Log = new Logger(modEntry, this.Log);

            var baseOnLoad = base.OnLoad(modEntry, harmony, harmonyPatch);

            OnUnload = modEntry =>
            {
                if(modEntry.Info.Id == this.ModEntry.Info.Id)
                { 
                    harmony?.UnpatchAll(modEntry.Info.Id);
                }

                return true;
            };

            return baseOnLoad;
        }
    }

#if DEBUG
    [EnableReloading]
#endif
    static class Main
    {
        internal static readonly MiscTweaksMod Mod = new();

        internal static Microsoftenator.Wotr.Common.ModTemplate.Logger Log => Mod.Log;

        internal static bool Enabled { get; private set; } = false;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            if (UnityModManager.FindMod("AlternateHumanTraits") is UnityModManager.ModEntry other)
            {
                var otherVersion = other.Version;
                if (otherVersion < Version.Parse("1.0.1"))
                {
                    modEntry.Logger.Critical($"Incompatible mod version of 'AlternateHumanTraits': {otherVersion} version >= 1.0.1 required.");
                    return false;
                }
            }

            Log.Debug($"{nameof(Main)}.{nameof(Load)}");

            var harmony = new Harmony(modEntry.Info.Id);

            Mod.OnLoad(modEntry, harmony);

            harmony.PatchAll();

            return true;
        }
    }

    internal static class Localization
    {
        private static readonly Lazy<LocalizedStringsPack> defaultStringsLazy = new(() => new(LocalizationManager.CurrentLocale));
        public static LocalizedStringsPack Default => defaultStringsLazy.Value;

        internal static LocalizedString CreateString(string key, string str)
        {
            Localization.Default.Add(key, str);

            return Localization.Default.Get(key) ?? throw new NullReferenceException();
        }
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    [HarmonyAfter("TabletopTweaks-Core", "TabletopTweaks-Base")]
    internal class BlueprintsCache_Init_Patch
    {
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

            MiscTweaksMod.ModSettings.SettingsInit();

            ReformedFiendDamageReductionGood.PatchDamageReduction();
            BookOfDreams.BookOfDreamsFix.BookOfDreamsUpgradeFix();
            StrengthBlessingMajor.StrengthBlessingMajorBuff.ArmorSpeedFix();
            //PrimalistBloodlineFixes.PatchPrimalistProgression();

            //BloodragerDraconicBaseBuffFixes.FixBloodragerDraconicClawsBuff();

            Localization.Default.LoadAll();
        }
    }
}
