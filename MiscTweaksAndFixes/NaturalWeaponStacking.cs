using System;
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

                if (size != newSize)
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
            bpuf.Components.OfType<EmptyHandWeaponOverride>().ToArray();
        public static IEnumerable<AddSecondaryAttacks> AddSecondaryAttacks(BlueprintUnitFact bpuf) =>
            bpuf.Components.OfType<AddSecondaryAttacks>().ToArray();
        public static IEnumerable<AddAdditionalLimb> AddAdditionalLimbs(BlueprintUnitFact bpuf) =>
            bpuf.Components.OfType<AddAdditionalLimb>().ToArray();

        public static (IEnumerable<EmptyHandWeaponOverride> emptyHandWeapons,
            IEnumerable<AddSecondaryAttacks> secondaryAttacks,
            IEnumerable<AddAdditionalLimb> additionalLimbs) GetNaturalWeapons(UnitEntityData unitData, WeaponCategory weaponCategory)
        {
            var buffs =
                unitData.Buffs.Enumerable
                    .Select(buff => buff.Blueprint)
                    .ToArray();

            var facts =
                unitData.Facts.List
                    .OfType<UnitFact>()
                    .Where(f => f is not Buff)
                    .Select(f => f.Blueprint)
                    .Concat(buffs)
                    .ToArray();

            var emptyHandWeapons =
                facts.SelectMany(EmptyHandWeaponOverrides);

            var secondaryAttacks =
                facts.SelectMany(AddSecondaryAttacks);

            var additionalLimbs =
                facts.SelectMany(AddAdditionalLimbs);

            Main.Log.Debug($"EmptyHandOverride: {emptyHandWeapons.Count()}, " +
                $"AddSecondaryAttacks: {secondaryAttacks.Count()}, " +
                $"AddAdditionalLimb: {additionalLimbs.Count()}");

#if DEBUG
            if (emptyHandWeapons.Count() > 0)
            {
                Main.Log.Debug("Empty hand overrides:");
                foreach (var eho in facts.Where(f => f.Components.OfType<EmptyHandWeaponOverride>().Any()))
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
                foreach (var sa in facts.Where(f => f.Components.OfType<AddSecondaryAttacks>().Any()))
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
                foreach (var al in facts.Where(f => f.Components.OfType<AddAdditionalLimb>().Any()))
                {
                    Main.Log.Debug($"    {al.Name} - {al.AssetGuid}");
                    foreach (var c in al.Components.OfType<AddAdditionalLimb>())
                        Main.Log.Debug(
                            $"      {(c.Weapon.Category == weaponCategory ? "*" : " ")} " +
                            $"{c.Weapon.Name} ({c.Weapon.Category}) - {c.Weapon.AssetGuid}");
                }
            }
#endif

            return (emptyHandWeapons.Where(w => w.Weapon.Category == weaponCategory),
                secondaryAttacks.Where(sa => sa.Weapon.Any(w => w.Category == weaponCategory)),
                additionalLimbs.Where(al => al.Weapon.Category == weaponCategory));
        }

        public static IEnumerable<BlueprintItemWeapon> GetAllNaturalWeapons(UnitEntityData unit)
        {
            var facts = unit.Facts.List.OfType<UnitFact>().Select(f => f.Blueprint).ToArray();

            var emptyHandWeapons = facts.SelectMany(EmptyHandWeaponOverrides).Select(c => c.Weapon);
            var secondaryAttacks = facts.SelectMany(AddSecondaryAttacks).SelectMany(c => c.Weapon);
            var additionalLimbs = facts.SelectMany(AddAdditionalLimbs).Select(c => c.Weapon);

            return emptyHandWeapons.Concat(secondaryAttacks).Concat(additionalLimbs);
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
