using System;
using System.Collections.Generic;
using HarmonyLib;
using Reactor.Extensions;
using UnhollowerBaseLib;

namespace JesterPlugin
{
    public partial class JesterPlugin
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class SetJesterPatch
        {
            public static PlayerControl Jester;

            public static void Postfix(PlayerControl __instance,
                [HarmonyArgument(0)] Il2CppReferenceArray<GameData.PlayerInfo> impostors)
            {
                var crewmates = new List<PlayerControl>();
                foreach (var pctrl in PlayerControl.AllPlayerControls)
                {
                    bool wasImpostor = false;
                    foreach (var infected in impostors)
                    {
                        if (pctrl.PlayerId == infected.PlayerId)
                        {
                            wasImpostor = true;
                            break;
                        }
                    }

                    if (!wasImpostor)
                    {
                        crewmates.Add(pctrl);
                    }
                }

                var rand = new Random();
                var picked = crewmates[rand.Next(0, crewmates.Count)];
                Jester = picked;
                System.Console.WriteLine(
                    $"Rolled player {picked.name} {picked.PlayerId}");
                __instance.Send<SetJesterRpc>(new SetJesterRpc.Data(picked.PlayerId), true);
            }
        }

        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        public static class StartScreenPatch
        {
            public static void Prefix(IntroCutscene.CoBegin__d __instance)
            {
                if (SetJesterPatch.Jester.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    var inst = __instance.__this;
                    inst.Title.Text = "Jester";
                    inst.Title.Color = JesterColor;
                    inst.ImpostorText.Text = "Get voted out";
                    inst.BackgroundBar.material.color = JesterColor;
                    __instance.yourTeam =
                        new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                    __instance.yourTeam.Add(SetJesterPatch.Jester);
                }
            }
        }
    }
}