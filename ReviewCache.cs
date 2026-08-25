using System.Collections.Generic;

namespace Votes
{
    public static class ReviewCache
    {
        public class ReviewData
        {
            public int Score;
            public bool AlreadyVoted;
            public bool Loaded;
        }

        public static Dictionary<string, ReviewData> Scores = new Dictionary<string, ReviewData>();

        public static ReviewData GetOrDefault(string userId)
        {
            ReviewData data;
            if (Scores.TryGetValue(userId, out data))
                return data;
            return new ReviewData { Score = 0, AlreadyVoted = false, Loaded = false };
        }
    }
}