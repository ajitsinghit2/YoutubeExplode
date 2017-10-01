﻿using System;
using System.Collections.Generic;

namespace YoutubeExplode.Models
{
    /// <summary>
    /// Playlist info
    /// </summary>
    public partial class Playlist
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Type
        /// </summary>
        public PlaylistType Type { get; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Author's display name
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Statistics
        /// </summary>
        public Statistics Statistics { get; }

        /// <summary>
        /// Videos in the playlist
        /// </summary>
        public IReadOnlyList<PlaylistVideo> Videos { get; }

        /// <inheritdoc />
        public Playlist(string id, string title, string author, string description, Statistics statistics,
            IReadOnlyList<PlaylistVideo> videos)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = GetPlaylistType(id);
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            Videos = videos ?? throw new ArgumentNullException(nameof(videos));
        }
    }

    public partial class Playlist
    {
        /// <summary>
        /// Get playlist type from playlist id
        /// </summary>
        protected static PlaylistType GetPlaylistType(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (id.StartsWith("PL", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.Normal;

            if (id.StartsWith("RD", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.VideoMix;

            if (id.StartsWith("UL", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.ChannelVideoMix;

            if (id.StartsWith("UU", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.ChannelVideos;

            if (id.StartsWith("PU", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.PopularChannelVideos;

            if (id.StartsWith("LL", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.LikedVideos;

            if (id.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.Favorites;

            if (id.StartsWith("WL", StringComparison.OrdinalIgnoreCase))
                return PlaylistType.WatchLater;

            throw new ArgumentOutOfRangeException(nameof(id), $"Unexpected playlist ID [{id}]");
        }
    }
}