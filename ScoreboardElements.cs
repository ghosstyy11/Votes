using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using PlayFab;

namespace Votes
{
    public static class ScoreboardElements
    {
        public static Plugin.ReviewLineData EnsureElements(GorillaPlayerScoreboardLine instance)
        {
            Plugin.ReviewLineData existing;
            if (Plugin.ReviewLineRefs.TryGetValue(instance, out existing))
                return existing;

            GameObject upvoteObj = Object.Instantiate(instance.muteButton.gameObject, instance.muteButton.transform.parent);
            GameObject downvoteObj = Object.Instantiate(instance.muteButton.gameObject, instance.muteButton.transform.parent);
            upvoteObj.name = "UpVote";
            downvoteObj.name = "DownVote";
            upvoteObj.transform.localPosition = new Vector3(21f, 0f, 0f);
            downvoteObj.transform.localPosition = new Vector3(33f, 0f, 0f);
            upvoteObj.transform.localScale = new Vector3(10, 10, 5);
            downvoteObj.transform.localScale = new Vector3(10, 10, 5);
            upvoteObj.GetComponent<Renderer>().material.color = new Color(0.5f, 1f, 0.5f);
            downvoteObj.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.5f);

            GameObject upvoteTextObj = new GameObject("UpvoteText");
            upvoteTextObj.transform.SetParent(upvoteObj.transform, false);
            upvoteTextObj.transform.localPosition = new Vector3(0f, 0f, -0.505f);
            upvoteTextObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
            TextMeshPro upvoteText = upvoteTextObj.AddComponent<TextMeshPro>();
            upvoteText.alignment = TextAlignmentOptions.Center;
            upvoteText.text = ">";
            upvoteText.font = Plugin.Instance.utopium;
            upvoteText.fontSize = 8f;

            GameObject downvoteTextObj = new GameObject("DownvoteText");
            downvoteTextObj.transform.SetParent(downvoteObj.transform, false);
            downvoteTextObj.transform.localPosition = new Vector3(0f, 0f, -0.505f);
            downvoteTextObj.transform.localRotation = Quaternion.Euler(0, 0, -90);
            TextMeshPro downvoteText = downvoteTextObj.AddComponent<TextMeshPro>();
            downvoteText.alignment = TextAlignmentOptions.Center;
            downvoteText.text = ">";
            downvoteText.font = Plugin.Instance.utopium;
            downvoteText.fontSize = 8f;

            upvoteObj.SetActive(true);
            downvoteObj.SetActive(true);

            Object.Destroy(upvoteObj.GetComponent<GorillaPlayerLineButton>());
            Object.Destroy(downvoteObj.GetComponent<GorillaPlayerLineButton>());

            GorillaPressableButton upvoteButton = upvoteObj.AddComponent<GorillaPressableButton>();
            GorillaPressableButton downvoteButton = downvoteObj.AddComponent<GorillaPressableButton>();

            GameObject textObj = new GameObject("ReviewScoreText");
            textObj.transform.SetParent(instance.muteButton.transform.parent, false);
            textObj.transform.localPosition = new Vector3(-14f, 0f, 0f);
            TextMeshProUGUI scoreText = textObj.AddComponent<TextMeshProUGUI>();
            scoreText.fontSize = 8f;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.text = "0";
            scoreText.characterSpacing = -15f;
            scoreText.font = Plugin.Instance.utopium;

            Transform gizmoSpeaker = instance.transform.Find("gizmo-speaker");
            gizmoSpeaker.localPosition = new Vector3(-33.4f, 0, 0);
            gizmoSpeaker.gameObject.GetComponent<SpriteRenderer>().material.renderQueue = 3001;

            upvoteButton.gameObject.GetComponent<Collider>().enabled = true;
            downvoteButton.gameObject.GetComponent<Collider>().enabled = true;

            upvoteButton.onPressed += (button, isLeftHand) =>
            {
                string voterId = PlayFabSettings.staticPlayer.EntityId; // this needs to be entity id not user id
                string voterToken = PlayFabSettings.staticPlayer.EntityToken; // i explain why this is here below!
                string targetId = instance.linePlayer.UserId;

                ReviewCache.ReviewData data = ReviewCache.GetOrDefault(targetId);
                data.Score += 1;
                data.AlreadyVoted = true;
                ReviewCache.Scores[targetId] = data;
                instance.InitializeLine();

                SubmitVote(voterId, voterToken, targetId, true);
            };

            downvoteButton.onPressed += (button, isLeftHand) =>
            {
                string voterId = PlayFabSettings.staticPlayer.EntityId;
                string voterToken = PlayFabSettings.staticPlayer.EntityToken; // i dont want to write two of the same comment
                string targetId = instance.linePlayer.UserId;

                ReviewCache.ReviewData data = ReviewCache.GetOrDefault(targetId);
                data.Score -= 1;
                data.AlreadyVoted = true;
                ReviewCache.Scores[targetId] = data;
                instance.InitializeLine();

                SubmitVote(voterId, voterToken, targetId, false);
            };

            Plugin.ReviewLineData refs = new Plugin.ReviewLineData
            {
                UpvoteButton = upvoteButton,
                DownvoteButton = downvoteButton,
                ScoreText = scoreText
            };
            Plugin.ReviewLineRefs[instance] = refs;
            return refs;
        }

        private static void SubmitVote(string voterId, string voterToken, string targetId, bool isUpvote)
        {
            if (Plugin.Instance != null)
                Plugin.Instance.StartCoroutine(SubmitVoteCoroutine(voterId, voterToken, targetId, isUpvote));
        }

        private static IEnumerator SubmitVoteCoroutine(string voterId, string voterToken, string targetId, bool isUpvote)
        {
            string url = "https://api.ghosty.uk/reviews/add"; // hey pretty please don't abuse this there's no need to ruin it for everyone else

            // with the changes, it'll be possible to vote yourself, but honestly i don't really care
            if (targetId == NetworkSystem.Instance.LocalPlayer.UserId) yield break;
            // good enough

            VoteRequest body = new VoteRequest
            {
                voter_id = voterId,
                voter_token = voterToken, // yes this looks sketchy as fuck, but all its used for is firing the current version cloudscript making sure the id is real to stop people from spamming votes. if you don't want to use the mod because this is here, don't use it.
                target_id = targetId,
                isUpvote = isUpvote
            };
            string json = JsonUtility.ToJson(body);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Reviews] Failed to submit vote for {targetId}: {req.error}");
                    yield break;
                }

                // hooray
            }
        }

        [System.Serializable]
        private class VoteRequest
        {
            public string voter_id;
            public string voter_token;
            public string target_id;
            public bool isUpvote;
        }
    }
}