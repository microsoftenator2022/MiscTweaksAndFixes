﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._VM.Other;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.Utility.UnitDescription;

using Microsoftenator.Wotr.Common.Blueprints;
using Microsoftenator.Wotr.Common.Blueprints.Extensions;
using Microsoftenator.Wotr.Common.Util;

using MiscTweaksAndFixes.Bloodrager;

namespace MiscTweaksAndFixes.NaturalWeaponStacking
{
    internal static class NaturalWeaponStacking
    {
        // Automatically identify natural weapons on equip
        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.OnDidEquipped))]
        internal class ItemEntity_OnDidEquipped_Patch
        {
            static void Prefix(UnitEntityData wielder, ItemEntity __instance)
            {
                if (!wielder.IsPlayerFaction) return;

                if (__instance is ItemEntityWeapon weapon && weapon.Blueprint.IsNatural)
                    weapon.Identify();
            }
        }

        [HarmonyPatch(typeof(ItemEntityWeapon), nameof(ItemEntityWeapon.Size), MethodType.Getter)]
        internal class ItemEntityWeapon_get_Size_Patch
        {
            static Size Postfix(Size size, ItemEntityWeapon __instance)
            {
                if(!PatchConditions(__instance.Wielder)) return size;

                var blueprint = __instance.Blueprint;

                if(!blueprint.IsNatural || blueprint.IsUnarmed) return size;

                Main.Log.Debug($"{nameof(ItemEntityWeapon_get_Size_Patch)}.{nameof(Postfix)}");

                Main.Log.Debug($"Blueprint: {blueprint.Name} - {blueprint.AssetGuid}");
                Main.Log.Debug($"Type: {blueprint.Type.TypeName}");
                Main.Log.Debug($"Wielder: {__instance.Wielder.CharacterName}");

                var (emptyHandWeapons,
                    secondaryAttacks,
                    additionalLimbs) = GetNaturalWeapons(__instance.Wielder, blueprint.Category);
                
                var sizeIncrease = emptyHandWeapons.Count()
                    + additionalLimbs.Count()
                    + secondaryAttacks.Select(sa => sa.Weapon.Count()).Sum()
                    - 1;

                var newSize = size + Math.Max(0, sizeIncrease);

                Main.Log.Debug($"Size change = {sizeIncrease}: " +
                    $"{WeaponDamageScaleTable.Scale(blueprint.BaseDamage, size, blueprint.Size)} ({size}) " +
                    $"-> {WeaponDamageScaleTable.Scale(blueprint.BaseDamage, newSize, blueprint.Size)} ({newSize})");

                return newSize;
            }
        }

        [HarmonyPatch(typeof(RuleAttackWithWeapon), nameof(RuleAttackWithWeapon.OnTrigger))]
        internal class RuleAttackWithWeapon_OnTrigger_Patch
        {
            static void Prefix(RulebookEventContext context,
                RuleAttackWithWeapon __instance,
                out List<(ItemEnchantment, MechanicsContext.Data)> __state)
            {
                __state = new();

                var blueprint = __instance.Weapon.Blueprint;
                var unit = __instance.Weapon.Wielder;

                if(!PatchConditions(unit) || !blueprint.IsNatural || blueprint.IsUnarmed) return;

                Main.Log.Debug($"{nameof(RuleAttackWithWeapon_OnTrigger_Patch)}.{nameof(Prefix)}");

                foreach (var weapon in GetAllNaturalWeapons(unit).Where(w => w.Category == blueprint.Category))
                {
                    foreach(var enchantBlueprint in weapon.Enchantments)
                    {
                        if(__instance.Weapon.Enchantments.Any(e => e.Blueprint.AssetGuid == enchantBlueprint.AssetGuid)) continue;

                        Main.Log.Debug($"Adding enchant {enchantBlueprint.Name} '{enchantBlueprint.AssetGuid}'" +
                            $" from weapon {weapon.Name}'{weapon.AssetGuid}'");
                        
                        UnitEntityData initiator = __instance.Weapon.Wielder;

                        var enchantContextData =
                            ContextData<MechanicsContext.Data>.Request()
                                .Setup(new MechanicsContext(null, initiator, enchantBlueprint), initiator);

                        __state.Add((__instance.Weapon.AddEnchantment(enchantBlueprint, enchantContextData.Context), enchantContextData));
                    }
                }
            }

            static void Postfix(RulebookEventContext context,
                RuleAttackWithWeapon __instance,
                List<(ItemEnchantment enchantment, MechanicsContext.Data contextData)> __state)
            {
                var blueprint = __instance.Weapon.Blueprint;
                var unit = __instance.Weapon.Wielder;

                if (!PatchConditions(unit) || !blueprint.IsNatural || blueprint.IsUnarmed) return;

                Main.Log.Debug($"{nameof(RuleAttackWithWeapon_OnTrigger_Patch)}.{nameof(Postfix)}");

                if (__state is null || __state.Count() == 0) return;

                foreach(var enchant in __state)
                {
                    Main.Log.Debug($"Removing enchant {enchant.enchantment.Name} '{enchant.enchantment.Blueprint.AssetGuid}'");

                    __instance.Weapon.RemoveEnchantment(enchant.enchantment);
                }

                __state.Clear();
            }
        }

        public static IEnumerable<EmptyHandWeaponOverride> EmptyHandWeaponOverrides(BlueprintUnitFact bpuf) =>
            bpuf.ComponentsArray.OfType<EmptyHandWeaponOverride>().ToArray();
        public static IEnumerable<AddSecondaryAttacks> AddSecondaryAttacks(BlueprintUnitFact bpuf) =>
            bpuf.ComponentsArray.OfType<AddSecondaryAttacks>().ToArray();
        public static IEnumerable<AddAdditionalLimb> AddAdditionalLimbs(BlueprintUnitFact bpuf) =>
            bpuf.ComponentsArray.OfType<AddAdditionalLimb>().ToArray();

        public static (BlueprintUnitFact[] emptyHandWeaponOverrides,
            BlueprintUnitFact[] secondaryAttacks, BlueprintUnitFact[] additionalLimbs)
        GetNaturalWeaponsForUnit(UnitEntityData unit)
        {
            Main.Log.Debug($"{nameof(NaturalWeaponStacking)}.{nameof(GetNaturalWeaponsForUnit)}");

            var buffs =
                unit.Buffs.Enumerable
                    .Select(buff => buff.Blueprint)
                    .ToArray();

            var facts =
                unit.Facts.List
                    .OfType<UnitFact>()
                    .Where(f => f is not Buff)
                    .Select(f => f.Blueprint)
                    .Concat(buffs)
                    .ToArray();

            // Special case for bloodrager draconic claws
            if (unit.Buffs.HasFact(OwlcatBlueprints.BloodragerDraconicBaseBuff.GetBlueprint()))
            {
                Main.Log.Debug("Draconic bloodrager handler");

                var bloodragerClawBuffs =
                    BloodlineClawsBuffs.GetBloodragerDragonClawFeaturesFor(unit)
                        .Select(f => BloodlineClawsBuffs.FeatureBuffMap[f])
                        .ToList();
                
                if(Main.Mod.Settings.DebugLogging)
                {
                    foreach(var buff in bloodragerClawBuffs)
                        Main.Log.Debug($"Natural weapon buff: {buff.Name} - {buff.AssetGuid}");
                }

                if (facts.FirstOrDefault(b => bloodragerClawBuffs.Contains(b)) is BlueprintBuff b)
                    bloodragerClawBuffs.Remove(b);

                var factsList = facts.ToList();

                foreach (var buff in bloodragerClawBuffs)
                {
                    Main.Log.Debug($"Selected extra natural weapon buff: {buff.Name} - {buff.AssetGuid}");
                    factsList.Add(buff);
                }

                facts = factsList.ToArray();
            }

            var ehwo = facts.Where(f => f.Components.OfType<EmptyHandWeaponOverride>().Any()).ToArray();
            var asa = facts.Where(f => f.Components.OfType<AddSecondaryAttacks>().Any()).ToArray();
            var aal = facts.Where(f => f.Components.OfType<AddAdditionalLimb>().Any()).ToArray();

            return
            (
                ehwo,
                asa,
                aal
            );
        }

        public static IEnumerable<BlueprintItemWeapon> GetAllNaturalWeapons(UnitEntityData unit)
        {
            var facts = GetNaturalWeaponsForUnit(unit);

            var emptyHandWeapons = facts.emptyHandWeaponOverrides.SelectMany(EmptyHandWeaponOverrides).Select(c => c.Weapon);
            var secondaryAttacks = facts.secondaryAttacks.SelectMany(AddSecondaryAttacks).SelectMany(c => c.Weapon);
            var additionalLimbs = facts.additionalLimbs.SelectMany(AddAdditionalLimbs).Select(c => c.Weapon);

            return emptyHandWeapons.Concat(secondaryAttacks).Concat(additionalLimbs);
        }

        public static (IEnumerable<EmptyHandWeaponOverride> emptyHandWeapons,
            IEnumerable<AddSecondaryAttacks> secondaryAttacks,
            IEnumerable<AddAdditionalLimb> additionalLimbs) GetNaturalWeapons(UnitEntityData unitData, WeaponCategory weaponCategory)
        {
            Main.Log.Debug($"{nameof(NaturalWeaponStacking)}.{nameof(GetNaturalWeapons)}");

            var (emptyHandWeapons, secondaryAttacks, additionalLimbs) = GetNaturalWeaponsForUnit(unitData);


            #region Debug Logging
            if (Main.Mod.Settings.DebugLogging)
            {
                Main.Log.Debug($"EmptyHandOverride: {emptyHandWeapons.Count()}, " +
                    $"AddSecondaryAttacks: {secondaryAttacks.Count()}, " +
                    $"AddAdditionalLimb: {additionalLimbs.Count()}");

                if (emptyHandWeapons.Count() > 0)
                {
                    Main.Log.Debug("Empty hand overrides:");
                    foreach (var eho in emptyHandWeapons)
                    {
                        Main.Log.Debug($"    {eho.Name} - {eho.AssetGuid}");
                        foreach (var c in eho.Components.OfType<EmptyHandWeaponOverride>())
                            Main.Log.Debug(
                                $"      {(c.Weapon.Category == weaponCategory ? "*" : " ")} " +
                                $"{c.Weapon.Name} ({c.Weapon.Category}) - {c.Weapon.AssetGuid}");
                    }
                }

                if (secondaryAttacks.Count() > 0)
                {
                    Main.Log.Debug("Secondary attacks:");
                    foreach (var sa in secondaryAttacks)
                    {
                        Main.Log.Debug($"    {sa.Name} - {sa.AssetGuid}");
                        foreach (var w in sa.Components.OfType<AddSecondaryAttacks>().SelectMany(c => c.Weapon))
                        {
                            Main.Log.Debug(
                                $"      {(w.Category == weaponCategory ? "*" : " ")} " +
                                $"{w.Name} ({w.Category}) - {w.AssetGuid}");
                        }

                    }
                }

                if (additionalLimbs.Count() > 0)
                {
                    Main.Log.Debug("Additional Limbs:");
                    foreach (var al in additionalLimbs)
                    {
                        Main.Log.Debug($"    {al.Name} - {al.AssetGuid}");
                        foreach (var c in al.Components.OfType<AddAdditionalLimb>())
                            Main.Log.Debug(
                                $"      {(c.Weapon.Category == weaponCategory ? "*" : " ")} " +
                                $"{c.Weapon.Name} ({c.Weapon.Category}) - {c.Weapon.AssetGuid}");
                    }
                }
            }
            #endregion

            return (emptyHandWeapons.SelectMany(EmptyHandWeaponOverrides).Where(w => w.Weapon.Category == weaponCategory),
                secondaryAttacks.SelectMany(AddSecondaryAttacks).Where(sa => sa.Weapon.Any(w => w.Category == weaponCategory)),
                additionalLimbs.SelectMany(AddAdditionalLimbs).Where(al => al.Weapon.Category == weaponCategory));
        }

        public static bool Enabled { get; internal set; } = true;

        //TODO: Do these conditions make sense?
        internal static bool PatchConditions(UnitEntityData unit)
        {
            if(!Enabled) return false;

            if(!unit.IsPlayerFaction) return false;
            if(unit.IsPet) return false;
            if(unit.Body.IsPolymorphed) return false;

            return true;
        }
    }
}
