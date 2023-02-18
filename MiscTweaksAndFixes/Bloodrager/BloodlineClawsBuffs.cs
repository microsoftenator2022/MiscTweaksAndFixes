using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

using Microsoftenator.Wotr.Common;
using Microsoftenator.Wotr.Common.Blueprints;
using Microsoftenator.Wotr.Common.Blueprints.Extensions;
using Microsoftenator.Wotr.Common.Util;

using MoreLinq;

namespace MiscTweaksAndFixes
{
    public static partial class OwlcatBlueprints
    {
        public static partial class Guids
        {
            public const string BloodragerDraconicBaseFeature = "213290bab56cc1b4eb67eab54d893956";

            public const string BloodragerDraconicClawFeature1 = "419dcd95d81401e4eaebbfb4779b0135";
            public const string BloodragerDraconiclClawFeature4 = "385807df7d8f64845972e2636dfcc61e";
            public const string BloodragerDraconicClawFeature8 = "bb1ddcc1c76e2184d87f7402a0b100d7";

            public const string BloodragerDraconicClawFeature12Acid = "fdc6fd17009e61841a37f19deab50276";
            public const string BloodragerDraconicClawFeature12Cold = "3c7ad8aeb928f4143b7acc6f312a5671";
            public const string BloodragerDraconicClawFeature12Electricity = "883412103cd588048b8bef5d76166e18";
            public const string BloodragerDraconicClawFeature12Fire = "dea42f81cb4a6c54fb2ba6395df7694f";

            public const string BloodragerDraconicClaw1d6 = "80cacb202e95c0946ab0fe03e3e5dd4d";
            public const string BloodragerDraconicClaw1d6Magic = "88bbb67777404b646950fc2445287726";
            public const string BloodragerDraconicClaw1d8Magic = "d32d1be18b188b04fb551b14d5bc9d1f";

            public const string BloodragerDraconicClaw1d8MagicAcid = "2e4ea327ad2330d40807b0d9c3492896";
            public const string BloodragerDraconicClaw1d8MagicCold = "d454bd839a8489d4fad2f168fd7f64ae";
            public const string BloodragerDraconicClaw1d8MagicElectricity = "f05aebe28062a1645992093a92426c61";
            public const string BloodragerDraconicClaw1d8MagicFire = "6910e7deae4531d4bac0b8c0b7dbfde4";

            public const string BloodragerDraconicBaseBuff = "feff8e3877842f04c814d88dad8c8e7b";

            public const string BloodragerDraconicClawBuff1 = "58e75d07ac64a7a42bf56567232837c8";
            public const string BloodragerDraconicClawBuff4 = "7fe779dd47789db439527d683bbf1cc8";
            public const string BloodragerDraconicClawBuff8 = "a8afb547a2f268940a390bb6cd754e6d";

            public const string BloodragerDraconicClawBuff12Acid = "282c65d891dbcfa4dbcc779207d4db5c";
            public const string BloodragerDraconicClawBuff12Cold = "ec1b211db643a5a40b9cd387f69e0ba0";
            public const string BloodragerDraconicClawBuff12Electricity = "75a8655024e048a428481bb61350f400";
            public const string BloodragerDraconicClawBuff12Fire = "829f1a4ac115382409067251c2fc9353";
        }

        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconicBaseFeature = new(Guids.BloodragerDraconicBaseFeature);

        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconicClawFeature1 = new(Guids.BloodragerDraconicClawFeature1);
        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconiclClawFeature4 = new(Guids.BloodragerDraconiclClawFeature4);
        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconicClawFeature8 = new(Guids.BloodragerDraconicClawFeature8);

        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconicClawFeature12Acid = new(Guids.BloodragerDraconicClawFeature12Acid);
        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconicClawFeature12Cold = new(Guids.BloodragerDraconicClawFeature12Cold);
        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconicClawFeature12Electricity = new(Guids.BloodragerDraconicClawFeature12Electricity);
        public static readonly OwlcatBlueprint<BlueprintFeature> BloodragerDraconicClawFeature12Fire = new(Guids.BloodragerDraconicClawFeature12Fire);

        public static readonly OwlcatBlueprint<BlueprintItemWeapon> BloodragerDraconicClaw1d6 = new(Guids.BloodragerDraconicClaw1d6);
        public static readonly OwlcatBlueprint<BlueprintItemWeapon> BloodragerDraconicClaw1d6Magic = new(Guids.BloodragerDraconicClaw1d6Magic);
        public static readonly OwlcatBlueprint<BlueprintItemWeapon> BloodragerDraconicClaw1d8Magic = new(Guids.BloodragerDraconicClaw1d8Magic);
        public static readonly OwlcatBlueprint<BlueprintItemWeapon> BloodragerDraconicClaw1d8MagicAcid = new(Guids.BloodragerDraconicClaw1d8MagicAcid);
        public static readonly OwlcatBlueprint<BlueprintItemWeapon> BloodragerDraconicClaw1d8MagicCold = new(Guids.BloodragerDraconicClaw1d8MagicCold);
        public static readonly OwlcatBlueprint<BlueprintItemWeapon> BloodragerDraconicClaw1d8MagicElectricity = new(Guids.BloodragerDraconicClaw1d8MagicElectricity);
        public static readonly OwlcatBlueprint<BlueprintItemWeapon> BloodragerDraconicClaw1d8MagicFire = new(Guids.BloodragerDraconicClaw1d8MagicFire);

        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicBaseBuff = new(Guids.BloodragerDraconicBaseBuff);

        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicClawBuff1 = new(Guids.BloodragerDraconicClawBuff1);
        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicClawBuff4 = new(Guids.BloodragerDraconicClawBuff4);
        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicClawBuff8 = new(Guids.BloodragerDraconicClawBuff8);
        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicClawBuff12Acid = new(Guids.BloodragerDraconicClawBuff12Acid);
        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicClawBuff12Cold = new(Guids.BloodragerDraconicClawBuff12Cold);
        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicClawBuff12Electricity = new(Guids.BloodragerDraconicClawBuff12Electricity);
        public static readonly OwlcatBlueprint<BlueprintBuff> BloodragerDraconicClawBuff12Fire = new(Guids.BloodragerDraconicClawBuff12Fire);
    }
}

namespace MiscTweaksAndFixes.Bloodrager
{
    internal static partial class Util
    {
        public static IEnumerable<Conditional> Flatten(this Conditional root)
        {
            var stack = new Stack<Conditional>();

            stack.Push(root);

            while (!stack.Empty())
            {
                var head = stack.Pop();
                yield return head;

                var ifTrue = head.IfTrue;
                var ifFalse = head.IfFalse;

                foreach (var c in ifTrue.Actions.OfType<Conditional>()) stack.Push(c);
                foreach (var c in ifFalse.Actions.OfType<Conditional>()) stack.Push(c);
            }
        }
    }

    public static class BloodlineClawsBuffs
    {
        private static readonly Lazy<BlueprintFeature[]> clawFeatures = new(() => new[]
        {
            OwlcatBlueprints.BloodragerDraconicClawFeature1.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconiclClawFeature4.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawFeature8.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawFeature12Acid.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawFeature12Cold.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawFeature12Electricity.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawFeature12Fire.GetBlueprint()
        });
        internal static IReadOnlyCollection<BlueprintFeature> ClawFeatures => clawFeatures.Value;

        private static readonly Lazy<BlueprintBuff[]> clawBuffs = new(() => new[]
        {
            OwlcatBlueprints.BloodragerDraconicClawBuff1.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawBuff4.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawBuff8.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawBuff12Acid.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawBuff12Cold.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawBuff12Electricity.GetBlueprint(),
            OwlcatBlueprints.BloodragerDraconicClawBuff12Fire.GetBlueprint(),
        });

        internal static IReadOnlyCollection<BlueprintBuff> ClawBuffs => clawBuffs.Value;

        private static readonly Lazy<Dictionary<BlueprintFeature, BlueprintBuff>> featureBuffMap = new(() => new()
        {
            { OwlcatBlueprints.BloodragerDraconicClawFeature1.GetBlueprint(),
                OwlcatBlueprints.BloodragerDraconicClawBuff1.GetBlueprint() },

            { OwlcatBlueprints.BloodragerDraconiclClawFeature4.GetBlueprint(),
                OwlcatBlueprints.BloodragerDraconicClawBuff4.GetBlueprint() },

            { OwlcatBlueprints.BloodragerDraconicClawFeature8.GetBlueprint(),
                OwlcatBlueprints.BloodragerDraconicClawBuff8.GetBlueprint() },

            { OwlcatBlueprints.BloodragerDraconicClawFeature12Acid.GetBlueprint(),
                OwlcatBlueprints.BloodragerDraconicClawBuff12Acid.GetBlueprint() },

            { OwlcatBlueprints.BloodragerDraconicClawFeature12Cold.GetBlueprint(),
                OwlcatBlueprints.BloodragerDraconicClawBuff12Cold.GetBlueprint() },

            {OwlcatBlueprints.BloodragerDraconicClawFeature12Electricity.GetBlueprint(),
                OwlcatBlueprints.BloodragerDraconicClawBuff12Electricity.GetBlueprint() },

            { OwlcatBlueprints.BloodragerDraconicClawFeature12Fire.GetBlueprint(),
                OwlcatBlueprints.BloodragerDraconicClawBuff12Fire.GetBlueprint() },
        });
        internal static IReadOnlyDictionary<BlueprintFeature, BlueprintBuff> FeatureBuffMap => featureBuffMap.Value;

        public static IDictionary<int, BlueprintFeature> GetBloodragerDraconicClawFeaturesByLevel(BlueprintProgression dragonBloodline) =>
            dragonBloodline.LevelEntries.SelectMany(le =>
                ClawFeatures
                    .Where(cf => le.Features.Contains(cf))
                    .Select(f => (le.Level, f)))
                .ToDictionary();

        public static IEnumerable<BlueprintFeature> GetBloodragerDragonClawFeaturesFor(UnitEntityData unit)
        {
            static bool IsBloodragerDraconicBloodline(BlueprintProgression progression)
            {
                var levelEntries = progression.LevelEntries.FirstOrDefault(le => le.Level == 1);
                
                if (levelEntries is null) return false;

                return levelEntries.Features
                        .Contains(OwlcatBlueprints.BloodragerDraconicBaseFeature.GetBlueprint());
            }

            var progressions = unit.Progression.m_Progressions.Values;

            if(Main.Mod.Settings.DebugLogging)
            {
                Main.Log.Debug($"Progressions:");
                foreach(var p in progressions)
                    Main.Log.Debug($"    {p.Blueprint?.name} -  {p.Blueprint?.AssetGuid}");
            }
            
            var features = progressions
                .Where(p => p.Blueprint is not null && IsBloodragerDraconicBloodline(p.Blueprint))
                .Select(p =>
                {
                    var bloodlineClawFeatures = GetBloodragerDraconicClawFeaturesByLevel(p.Blueprint);
                    return bloodlineClawFeatures[bloodlineClawFeatures.Keys
                        .OrderByDescending(Functional.Id)
                        .First(level => level <= p.Level)];
                });

            return features;
        }
    }

    public static class BloodragerDraconicBaseBuffFixes
    {
        internal static bool Enabled = true;

        internal static void FixBloodragerDraconicClawsBuff()
        {
            if (!Enabled) return;

            Main.Log.Debug($"{nameof(BloodragerDraconicBaseBuffFixes)}.{nameof(FixBloodragerDraconicClawsBuff)}");

            var clawsBuff4 = OwlcatBlueprints.BloodragerDraconicClawBuff4.GetBlueprint();

            // Buff weapon for level 4 is 1d8Magic, should be 1d6Magic
            if (clawsBuff4.Components[0] is EmptyHandWeaponOverride weaponComponent)
            {
                Main.Log.Debug($"Fixing {clawsBuff4.AssetGuid}");

                weaponComponent.m_Weapon =
                    OwlcatBlueprints.BloodragerDraconicClaw1d6Magic.GetBlueprint()
                        .ToReference<BlueprintItemWeaponReference>();
            }

            var bdbb = OwlcatBlueprints.BloodragerDraconicBaseBuff.GetBlueprint();

            if (bdbb.ComponentsArray[0] is not AddFactContextActions afca) return;

            if (afca.Activated.Actions.OfType<Conditional>().FirstOrDefault(c => c.Comment == "Claws")
                is not Conditional clawsConditional) return;

            var clawsBuff8 = OwlcatBlueprints.BloodragerDraconicClawBuff8.GetBlueprint();

            // Conditions for level 4 and 8 incorrectly check for the buff they should be adding instead of the features from the progression
            foreach (var condition in clawsConditional.Flatten())
            {
                if(condition.ConditionsChecker.Conditions.OfType<ContextConditionHasFact>().FirstOrDefault() 
                    is not ContextConditionHasFact cchf) continue;

                if (cchf.Fact == clawsBuff8)
                {
                    var clawFeature8 = OwlcatBlueprints.BloodragerDraconicClawFeature8.GetBlueprint();

                    Main.Log.Debug($"Replacing condition {cchf.Fact.AssetGuid} with {clawFeature8.AssetGuid}");

                    cchf.m_Fact = clawFeature8.ToReference<BlueprintUnitFactReference>();

                    continue;
                }

                if (cchf.Fact == clawsBuff4)
                {
                    var clawFeature4 = OwlcatBlueprints.BloodragerDraconiclClawFeature4.GetBlueprint();

                    Main.Log.Debug($"Replacing condition {cchf.Fact.AssetGuid} with {clawFeature4.AssetGuid}");

                    cchf.m_Fact = clawFeature4.ToReference<BlueprintUnitFactReference>();
                }
            }
        }
    }
}
