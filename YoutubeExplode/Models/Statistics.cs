using System;

namespace YoutubeExplode.Models
{
    /// <summary>
    /// User activity statistics
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// View count
        /// </summary>
        public long ViewCount { get; }

        /// <summary>
        /// Like count
        /// </summary>
        public long LikeCount { get; }

        /// <summary>
        /// Dislike count
        /// </summary>
        public long DislikeCount { get; }

        /// <summary>
        /// Average user rating in stars (1* to 5*)
        /// </summary>
        public double AverageRating => 1 + 4.0 * LikeCount / (LikeCount + DislikeCount);

        /// <inheritdoc />
        public Statistics(long viewCount, long likeCount, long dislikeCount)
        {
            if (viewCount < 0)
                throw new ArgumentOutOfRangeException(nameof(viewCount));
            if (likeCount < 0)
                throw new ArgumentOutOfRangeException(nameof(likeCount));
            if (dislikeCount < 0)
                throw new ArgumentOutOfRangeException(nameof(dislikeCount));

            ViewCount = viewCount;
            LikeCount = likeCount;
            DislikeCount = dislikeCount;
        }
    }
}