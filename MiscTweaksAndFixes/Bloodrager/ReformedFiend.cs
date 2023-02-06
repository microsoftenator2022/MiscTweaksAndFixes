using System;
using System.Collections.Generic;
using System.Linq;

using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Enums.Damage;

using Microsoftenator.Wotr.Common.Blueprints;
using Microsoftenator.Wotr.Common.Blueprints.Extensions;
using Microsoftenator.Wotr.Common;

namespace MiscTweaksAndFixes
{
    public static partial class OwlcatBlueprints
    {
        public static partial class Guids
        {
            public const string ReformedFiendDamageReductionFeature = "2a3243ad1ccf43d5a5d69de3f9d0420e";
        }

        public static readonly OwlcatBlueprint<BlueprintFeature> ReformedFiendDamageReductionFeature =
            new(Guids.ReformedFiendDamageReductionFeature);
    }
}

namespace MiscTweaksAndFixes.Bloodrager
{
    public static class ReformedFiendDamageReductionGood
    {
        public static bool Enabled { get; internal set; } = true;

        internal static void PatchDamageReduction()
        {
            Main.Log.Debug($"{nameof(ReformedFiendDamageReductionGood)}.{nameof(PatchDamageReduction)}");

            if(!Enabled)
            {
                Main.Log.Info($"{nameof(ReformedFiendDamageReductionGood)} DISABLED");

                return;
            }

            //Main.AddSharedBlueprints(new[] { OwlcatBlueprints.ReformedFiendDamageReductionFeature });
            SharedMods.Register(
                nameof(ReformedFiendDamageReductionGood), 
                1, 
                new [] { OwlcatBlueprints.ReformedFiendDamageReductionFeature });

            var bp = OwlcatBlueprints.ReformedFiendDamageReductionFeature.TryGetBlueprint();
            var description = bp?.Description;

            if (bp != null && description != null)
            {
                var dr = bp.GetComponent<AddDamageResistancePhysical>();

                dr.Alignment = DamageAlignment.Good;
                dr.BypassedByAlignment = true;

                description =
                    description
                        .Replace("Evil", "Good")
                        .Replace("evil", "good");

                bp.SetDescription(Localization.Default, description);
            }
        }
    }
}
