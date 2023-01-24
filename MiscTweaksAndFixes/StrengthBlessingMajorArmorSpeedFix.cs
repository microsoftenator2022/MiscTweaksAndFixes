using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;

using Microsoftenator.Wotr.Common.Blueprints;
using Microsoftenator.Wotr.Common.Blueprints.Extensions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.EntitySystem;
using Microsoftenator.Wotr.Common;

namespace MiscTweaksAndFixes.StrengthBlessingMajor
{
    public static partial class OwlcatBlueprints
    {
        public static partial class Guids
        {
            public const string StrengthBlessingMajorBuff = "833cdb09f6fc62f4888b3459f48b5854";
        }

        public static readonly OwlcatBlueprint<BlueprintBuff> StrengthBlessingMajorBuff =
            new(Guids.StrengthBlessingMajorBuff);
    }

    public class StrengthBlessingMajorBuff
    {
        public static bool Enabled { get; internal set; } = true;

        [AllowMultipleComponents]
        [AllowedOn(typeof(BlueprintBuff), false)]
        public class HeavyArmorSpeedPenaltyRemoval : ArmorSpeedPenaltyRemoval
        {
            public override void OnTurnOn()
            {
                base.Owner.State.Features.ImmuneToArmorSpeedPenalty.Retain();

                base.OnTurnOn();
            }

            public override void OnTurnOff()
            {
                base.Owner.State.Features.ImmuneToArmorSpeedPenalty.Release();

                base.OnTurnOff();
            }
        }

        public static void ArmorSpeedFix()
        {
            Main.Log.Debug($"{nameof(StrengthBlessingMajorBuff)}.{nameof(ArmorSpeedFix)}");

            if(!Enabled)
            {
                Main.Log.Info($"{nameof(StrengthBlessingMajorBuff)} DISABLED");

                return;
            }

            SharedMods.Register(nameof(StrengthBlessingMajorBuff), new[] { OwlcatBlueprints.StrengthBlessingMajorBuff });

            var buff = OwlcatBlueprints.StrengthBlessingMajorBuff.GetBlueprint();

            buff.RemoveComponents(c => c is ArmorSpeedPenaltyRemoval);
            buff.AddComponent(new HeavyArmorSpeedPenaltyRemoval());
        }
    }
}
