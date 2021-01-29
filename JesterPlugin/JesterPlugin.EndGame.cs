using HarmonyLib;
using Reactor.Extensions;
using UnityEngine;

namespace JesterPlugin
{
    public partial class JesterPlugin
    {
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
#if ITCH
                    ShipStatus.Method_34(GameOverReason.HumansDisconnect, false);
#elif STEAM
                    ShipStatus.RpcEndGame(GameOverReason.HumansDisconnect, false);
#endif
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