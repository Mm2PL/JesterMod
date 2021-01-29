using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using UnhollowerBaseLib;
using UnityEngine;
using Random = System.Random;

namespace JesterPlugin
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    public class JesterPlugin : BasePlugin
    {
        public const string Id = "pl.kotmisia.jesterrole";

        // public static Color JesterColor = new Color(1, (float) (204.0 / 255.0), 1, 1);
        // public static Color JesterColor = new Color(0, (float) (204.0 / 255.0), 0, 1);
        public static Color JesterColor =
            new Color((float) (240.0 / 255.0), (float) (125.0 / 255.0), (float) (13.0 / 255.0), 1);

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            RegisterCustomRpcAttribute.Register(this);
            Harmony.PatchAll();
        }


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

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class HudManagerPatch
        {
            public static void Postfix()
            {
                if (PlayerControl.LocalPlayer.PlayerId == SetJesterPatch.Jester.PlayerId)
                {
                    if (PlayerControl.LocalPlayer.nameText.Color == JesterColor) return;

                    PlayerControl.LocalPlayer.nameText.Color = PlayerControl.AllPlayerControls.Count > 1
                        ? Color.white
                        : JesterColor;
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        public static class MeetingPatch
        {
            public static void Postfix(MeetingHud __instance)
            {
                foreach (var pstate in __instance.playerStates)
                {
                    if (pstate.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId &&
                        PlayerControl.LocalPlayer.PlayerId == SetJesterPatch.Jester.PlayerId)
                    {
                        pstate.NameText.Color = JesterColor;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
        public static class ExileControllerDisplayPatch
        {
            public static void Postfix(ExileController __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
            {
                if (target.PlayerId == SetJesterPatch.Jester.PlayerId)
                {
                    if (PlayerControl.GameOptions.ConfirmEjects)
                    {
                        __instance.Field_9 = $"{target.PlayerName} was the Jester!";
                    }
                }

                System.Console.WriteLine(
                    $"xd {PlayerControl.GameOptions.ConfirmEjects} {PlayerControl.GameOptions.ConfirmImpostor}");
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
        public static class PlayerControlWinPatch
        {
            public static void Prefix()
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    ShipStatus.Instance.enabled = false;
                    PlayerControl.LocalPlayer.Send<SetJesterWin>(new SetJesterWin.Data("lolxd"), true);

                    HandleWinRpc();
                    ShipStatus.Method_34(GameOverReason.HumansDisconnect, false);
                }
            }

            public static void HandleWinRpc()
            {
                System.Console.WriteLine($"Handle win RPC!");
                if (PlayerControl.LocalPlayer.PlayerId == SetJesterPatch.Jester.PlayerId)
                {
                    EndGameScreenPatch.WinTextReplacement = "Victory";
                    EndGameScreenPatch.StingerReplacement = "crew";
                }
                else
                {
                    EndGameScreenPatch.WinTextReplacement = "Exiled jester";
                    EndGameScreenPatch.WinTextColorReplacement = JesterColor;
                    EndGameScreenPatch.StingerReplacement = "impostor";
                }

                EndGameScreenPatch.BGColor = JesterPlugin.JesterColor;
            }
        }


        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
        public static class EndGameScreenPatch
        {
            public static string WinTextReplacement;
            public static Color? WinTextColorReplacement;

            public static Color? BGColor;
            public static string StingerReplacement;

            static void Prefix(EndGameManager __instance)
            {
                ApplyOverrides(__instance, false);
            }

            public static void Postfix(EndGameManager __instance)
            {
                ApplyOverrides(__instance, true);
            }

            private static void ApplyOverrides(EndGameManager __instance, bool removeState)
            {
                __instance.DisconnectStinger = StingerReplacement switch
                {
                    "crew" => __instance.CrewStinger,
                    "impostor" => __instance.ImpostorStinger,
                    _ => __instance.DisconnectStinger
                };

                if (WinTextReplacement != null)
                {
                    __instance.WinText.Text = WinTextReplacement;
                }

                if (WinTextColorReplacement != null)
                {
                    __instance.WinText.Color = (Color) WinTextColorReplacement;
                }

                if (BGColor != null)
                {
                    __instance.BackgroundBar.material.color = (Color) BGColor;
                }

                if (removeState)
                {
                    WinTextReplacement = null;
                    WinTextColorReplacement = null;
                    BGColor = null;
                }
            }
        }
    }
}