using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Internal;
using YoutubeExplode.Models;

namespace YoutubeExplode
{
    public partial class YoutubeClient
    {
        /// <summary>
        /// Gets videos uploaded to a channel with given ID, truncating resulting video list at given number of pages (1 page ≤ 200 videos)
        /// </summary>
        public async Task<IReadOnlyList<PlaylistVideo>> GetChannelUploadsAsync(string channelId, int maxPages)
        {
            if (channelId == null)
                throw new ArgumentNullException(nameof(channelId));
            if (!ValidateChannelId(channelId))
                throw new ArgumentException("Invalid Youtube channel ID", nameof(channelId));
            if (maxPages <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPages), "Needs to be a positive number");

            // Compose a playlist ID
            var playlistId = "UU" + channelId.SubstringAfter("UC");

            // Get playlist info
            var playlistInfo = await GetPlaylistAsync(playlistId, maxPages).ConfigureAwait(false);

            return playlistInfo.Videos;
        }

        /// <summary>
        /// Gets videos uploaded to a channel with given ID
        /// </summary>
        public Task<IReadOnlyList<PlaylistVideo>> GetChannelUploadsAsync(string channelId)
            => GetChannelUploadsAsync(channelId, int.MaxValue);

        /// <summary>
        /// Gets channel info by ID
        /// </summary>
        public async Task<Channel> GetChannelAsync(string channelId)
        {
            if (channelId == null)
                throw new ArgumentNullException(nameof(channelId));
            if (!ValidateChannelId(channelId))
                throw new ArgumentException("Invalid Youtube channel ID", nameof(channelId));

            // Get channel uploads
            var uploads = await GetChannelUploadsAsync(channelId, 1).ConfigureAwait(false);
            var videoInfoSnippet = uploads.FirstOrDefault();
            if (videoInfoSnippet == null)
                throw new ParseException("Cannot get channel info because it doesn't have any uploaded videos");

            // Get video info of the first video
            var videoInfo = await GetVideoAsync(videoInfoSnippet.Id).ConfigureAwait(false);

            return videoInfo.Author;
        }
    }
}