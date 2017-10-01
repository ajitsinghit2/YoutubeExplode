using System;
using System.Collections.Generic;

namespace YoutubeExplode.Models
{
    /// <summary>
    /// Playlist video
    /// </summary>
    public class PlaylistVideo
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Thumbnails
        /// </summary>
        public VideoThumbnails Thumbnails { get; }

        /// <summary>
        /// Duration
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Search keywords
        /// </summary>
        public IReadOnlyList<string> Keywords { get; }

        /// <summary>
        /// Statistics
        /// </summary>
        public Statistics Statistics { get; }

        /// <inheritdoc />
        public PlaylistVideo(string id, string title, string description, VideoThumbnails thumbnails, TimeSpan duration,
            IReadOnlyList<string> keywords, Statistics statistics)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration));

            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
            Duration = duration;
            Keywords = keywords ?? throw new ArgumentNullException(nameof(keywords));
            Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        }
    }
}