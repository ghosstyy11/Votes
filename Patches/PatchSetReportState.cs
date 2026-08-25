using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Votes.Patches
{
    [HarmonyPatch(typeof(GorillaPlayerScoreboardLine), "SetReportState")]
    public class PatchSetReportState // make it so the buttons get hidden when the report buttons is pressed
    {
        public static void Postfix(GorillaPlayerScoreboardLine __instance)
        {
            Plugin.ReviewLineData refs;
            if (!Plugin.ReviewLineRefs.TryGetValue(__instance, out refs))
                return;

            bool isLocal = __instance.linePlayer != null && __instance.linePlayer.UserId == NetworkSystem.Instance.LocalPlayer.UserId;
            bool hide = isLocal || __instance.reportInProgress;

            refs.UpvoteButton.gameObject.SetActive(!hide);
            refs.DownvoteButton.gameObject.SetActive(!hide);
        }
    }
}
