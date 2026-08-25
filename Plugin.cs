using System.Collections.Generic;
using BepInEx;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System;
using PlayFab;

namespace Votes
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)] // funny graig mod template
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }

        public TMP_FontAsset utopium = null;


        public static Dictionary<GorillaPlayerScoreboardLine, ReviewLineData> ReviewLineRefs = new Dictionary<GorillaPlayerScoreboardLine, ReviewLineData>();
        public class ReviewLineData
        {
            public GorillaPressableButton UpvoteButton;
            public GorillaPressableButton DownvoteButton;
            public TextMeshProUGUI ScoreText;
        }


        void Start()
        {
            Instance = this;

            utopium = GameObject.Find("Board Text").GetComponent<TextMeshPro>().font; // good enough

            GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized()
        {
            NetworkSystem.Instance.OnJoinedRoomEvent += OnJoinedRoom;
            NetworkSystem.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkSystem.Instance.OnPlayerLeft += OnPlayerLeft;
        }

        void Update()
        {
            
        }

        private void OnPlayerLeft(NetPlayer player)
        {
            ReviewCache.Scores.Remove(player.UserId);
        }

        private void OnJoinedRoom()
        {
            List<string> userIds = new List<string>();
            foreach (NetPlayer player in NetworkSystem.Instance.PlayerListOthers)
            {
                userIds.Add(player.UserId);
            }
            FetchScoresForBatch(userIds);
        }

        private void OnPlayerJoined(NetPlayer player)
        {
            if (player.IsLocal) return;
            FetchScoresForBatch(new List<string> { player.UserId });
        }

        private void FetchScoresForBatch(List<string> userIds)
        {
            if (userIds.Count == 0) return;
            if (Plugin.Instance != null)
                Plugin.Instance.StartCoroutine(FetchScoresBatchCoroutine(userIds));
        }

        private IEnumerator FetchScoresBatchCoroutine(List<string> userIds)
        {
            string voterId = PlayFabSettings.staticPlayer.EntityId;
            string targetIdsParam = string.Join(",", userIds);
            string url = $"https://api.ghosty.uk/reviews/get?voter_id={UnityWebRequest.EscapeURL(voterId)}&target_ids={UnityWebRequest.EscapeURL(targetIdsParam)}";

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Reviews] Failed to fetch scores: {req.error}");
                    foreach (string userId in userIds)
                    {
                        if (!ReviewCache.Scores.ContainsKey(userId))
                            ReviewCache.Scores[userId] = new ReviewCache.ReviewData { Score = 0, AlreadyVoted = false, Loaded = false };
                    }
                    yield break;
                }

                List<ScoreEntry> entries;
                try
                {
                    entries = JsonConvert.DeserializeObject<List<ScoreEntry>>(req.downloadHandler.text); // fun fact i hate jsonutility it doesnt fucking work
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Reviews] Failed to parse response: {e.Message}, body: {req.downloadHandler.text}");
                    yield break;
                }

                if (entries == null) yield break;

                foreach (ScoreEntry entry in entries)
                {
                    ReviewCache.Scores[entry.userId] = new ReviewCache.ReviewData
                    {
                        Score = entry.score,
                        AlreadyVoted = entry.alreadyVoted,
                        Loaded = true
                    };
                    RefreshLineIfVisible(entry.userId);
                }
            }
        }

        private void RefreshLineIfVisible(string userId)
        {
            foreach (var kvp in Plugin.ReviewLineRefs)
            {
                GorillaPlayerScoreboardLine line = kvp.Key;
                if (line != null && line.linePlayer != null && line.linePlayer.UserId == userId)
                {
                    line.InitializeLine();
                }
            }
        }

        [Serializable]
        private class ScoreEntry
        {
            public string userId;
            public int score;
            public bool alreadyVoted;
        }
    }
}
