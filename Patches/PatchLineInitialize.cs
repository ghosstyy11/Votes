using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace Votes.Patches
{
    [HarmonyPatch(typeof(GorillaPlayerScoreboardLine), "InitializeLine")]
    public class PatchLineInitialize
    {
        public static void Postfix(GorillaPlayerScoreboardLine __instance)
        {
            Plugin.ReviewLineData refs = ScoreboardElements.EnsureElements(__instance);

            bool isLocal = __instance.linePlayer.UserId == NetworkSystem.Instance.LocalPlayer.UserId;
            refs.UpvoteButton.gameObject.SetActive(!isLocal);
            refs.DownvoteButton.gameObject.SetActive(!isLocal);
            refs.ScoreText.gameObject.SetActive(!isLocal);
            if (isLocal)
                return;

            ReviewCache.ReviewData data = ReviewCache.GetOrDefault(__instance.linePlayer.UserId);

            string prefix = "";
            if (data.Score > 0)
            {
                prefix = "<color=green>+";
            }
            else if (data.Score < 0)
            {
                prefix = "<color=red>"; // forgot that negative numbers already have a - lol
            }
            refs.ScoreText.text = prefix + data.Score.ToString();
            refs.UpvoteButton.ResetState();
            refs.DownvoteButton.ResetState();
            refs.UpvoteButton.enabled = !data.AlreadyVoted;
            refs.DownvoteButton.enabled = !data.AlreadyVoted;
            if (data.AlreadyVoted)
            {
                refs.UpvoteButton.gameObject.GetComponent<Renderer>().material.color = new Color(0.5f, 0.7f, 0.5f);
                refs.DownvoteButton.gameObject.GetComponent<Renderer>().material.color = new Color(0.7f, 0.5f, 0.5f);
                refs.UpvoteButton.gameObject.GetComponent<Collider>().enabled = false;
                refs.DownvoteButton.gameObject.GetComponent<Collider>().enabled = false;
            }
            else
            {
                refs.UpvoteButton.gameObject.GetComponent<Renderer>().material.color = new Color(0.5f, 1f, 0.5f); // messed up the colours somehow
                refs.DownvoteButton.gameObject.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.5f);
                refs.UpvoteButton.gameObject.GetComponent<Collider>().enabled = true;
                refs.DownvoteButton.gameObject.GetComponent<Collider>().enabled = true;
            }
            refs.UpvoteButton.gameObject.GetComponent<Renderer>().enabled = true;
            refs.DownvoteButton.gameObject.GetComponent<Renderer>().enabled = true;
        }
    }
}
