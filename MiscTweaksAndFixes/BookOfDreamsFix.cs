using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Persistence.Versioning;
using Kingmaker.PubSubSystem;

using Microsoftenator.Wotr.Common.Blueprints;
using Microsoftenator.Wotr.Common.Blueprints.Extensions;

namespace MiscTweaksAndFixes.BookOfDreams
{
    public static partial class OwlcatBlueprints
    {
        public static partial class Guids
        {
            // Not BlueprintScriptableObject
            public const string BookOfDreamsItemConvert_v2 = "8ea9114c683e4f218af674575aefcd57";
        }
    }

    // Quick and dirty attempt to fix Book of Dreams
    // Supposed to be longer needed after 2.0.1e (It seems to be broken still?)
    public static class BookOfDreamsFix
    {
        public static bool Enabled { get; internal set; } = true;

        private class EtudesUpdateEventHandler : IEtudesUpdateHandler
        {
            public readonly Action Action;

            public EtudesUpdateEventHandler(Action action) => this.Action = action;

            public void OnEtudesUpdate() => Action();
        }

        private class GameActionDelegate : GameAction
        {
            public readonly Action Action;
            private readonly string Caption;

            public GameActionDelegate(Action action, string caption = "")
            {
                this.Action = action;
                Caption = caption;
            }

            public override string GetCaption() => Caption;
            public override void RunAction() => this.Action();
        }

        private static void AddComponent<TComponent>(this BlueprintScriptableObject bp, TComponent component) where TComponent : BlueprintComponent
        {
            if (String.IsNullOrEmpty(component.name))
                component.name = $"{bp.AssetGuid}${typeof(TComponent)}${component.GetHashCode():x}";

            bp.ComponentsArray = bp.ComponentsArray.Append(component).ToArray();
        }

        internal static void BookOfDreamsUpgradeFix()
        {
            Main.Log.Debug($"{nameof(BookOfDreamsFix)}.{nameof(BookOfDreamsUpgradeFix)}");

            if(!Enabled)
            {
                Main.Log.Info($"{nameof(BookOfDreamsFix)} DISABLED");

                return;
            }

            //var BookOfDreamsItemConvert = "d62a0c903fa240308f95954c538e4eba";

            //var chapter2 = new OwlcatBlueprint<BlueprintEtude>(OwlcatBlueprints.Guids.BookOfDreams2ChapterBuff).GetBlueprint();
            //var chapter5 = new OwlcatBlueprint<BlueprintEtude>(OwlcatBlueprints.Guids.BookOfDreams2ChapterBuff).GetBlueprint();

            //var upgrader = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(BookOfDreamsItemConvert)) as BlueprintPlayerUpgrader;
            var upgrader = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(OwlcatBlueprints.Guids.BookOfDreamsItemConvert_v2)) as BlueprintPlayerUpgrader;

            if (upgrader == null)
            {
                Main.Log.Warning("Can't get get BookOfDreams upgrader");
            }
            else
            {
                //if (chapter2 == null || chapter5 == null)
                //{
                //    Main.Log.Debug("Can't get BookOfDreams etude");
                //    return;
                //}

                //chapter2.AddComponent(new EtudePlayTrigger()
                //{
                //    Actions = new ActionList() { Actions = new[] { new GameActionDelegate(() => upgrader2?.m_Actions.Run()) } }
                //});

                //chapter5.AddComponent(new EtudePlayTrigger()
                //{
                //    Actions = new ActionList() { Actions = new[] { new GameActionDelegate(() => upgrader2?.m_Actions.Run()) } }
                //});

                // Brute force approach. TODO: Figure out how to subscribe to EtudeSystem events
                EventBus.Subscribe(new EtudesUpdateEventHandler(() => upgrader.m_Actions.Run()));

                
            }
        }
    }
}
