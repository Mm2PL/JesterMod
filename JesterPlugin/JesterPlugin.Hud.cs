using HarmonyLib;
using UnityEngine;

namespace JesterPlugin
{
    public partial class JesterPlugin
    {
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
    }
}