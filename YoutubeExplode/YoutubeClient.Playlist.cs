using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using YoutubeExplode.Internal;
using YoutubeExplode.Models;
using YoutubeExplode.Services;

namespace YoutubeExplode
{
    public partial class YoutubeClient
    {
        /// <summary>
        /// Gets playlist info by ID, truncating resulting video list at given number of pages (1 page ≤ 200 videos)
        /// </summary>
        public async Task<Playlist> GetPlaylistAsync(string playlistId, int maxPages)
        {
            if (playlistId == null)
                throw new ArgumentNullException(nameof(playlistId));
            if (!ValidatePlaylistId(playlistId))
                throw new ArgumentException("Invalid Youtube playlist ID", nameof(playlistId));
            if (maxPages <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPages), "Needs to be a positive number");

            // Get all videos across pages
            var pagesDone = 0;
            var offset = 0;
            XElement playlistInfoXml;
            var videoIds = new HashSet<string>();
            var videos = new List<PlaylistVideo>();
            do
            {
                // Get
                var request = YoutubeHost + $"/list_ajax?style=xml&action_get_list=1&list={playlistId}&index={offset}";
                var response = await _httpService.GetStringAsync(request).ConfigureAwait(false);
                playlistInfoXml = XElement.Parse(response).StripNamespaces();

                // Parse videos
                var total = 0;
                var delta = 0;
                foreach (var videoInfoXml in playlistInfoXml.Elements("video"))
                {
                    // Basic info
                    var videoId = videoInfoXml.ElementStrict("encrypted_id").Value;
                    var videoTitle = videoInfoXml.ElementStrict("title").Value;
                    var videoThumbnails = new VideoThumbnails(videoId);
                    var videoDuration =
                        TimeSpan.FromSeconds(videoInfoXml.ElementStrict("length_seconds").Value.ParseDouble());
                    var videoDescription = videoInfoXml.ElementStrict("description").Value;

                    // Keywords
                    var videoKeywordsJoined = videoInfoXml.ElementStrict("keywords").Value;
                    var videoKeywords = Regex
                        .Matches(videoKeywordsJoined, @"(?<=(^|\s)(?<quote>""?))([^""]|(""""))*?(?=\<quote>(?=\s|$))")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .Where(s => s.IsNotBlank())
                        .ToArray();

                    // Statistics
                    var videoViewCount =
                        Regex.Replace(videoInfoXml.ElementStrict("views").Value, @"\D", "").ParseLong();
                    var videoLikeCount =
                        Regex.Replace(videoInfoXml.ElementStrict("likes").Value, @"\D", "").ParseLong();
                    var videoDislikeCount =
                        Regex.Replace(videoInfoXml.ElementStrict("dislikes").Value, @"\D", "").ParseLong();
                    var videoStatistics = new Statistics(videoViewCount, videoLikeCount, videoDislikeCount);

                    // Video
                    var video = new PlaylistVideo(videoId, videoTitle, videoDescription, videoThumbnails, videoDuration,
                        videoKeywords, videoStatistics);

                    // Add to list if not already there
                    if (videoIds.Add(video.Id))
                    {
                        videos.Add(video);
                        delta++;
                    }
                    total++;
                }

                // Break if the videos started repeating
                if (delta <= 0) break;

                // Prepare for next page
                pagesDone++;
                offset += total;
            } while (pagesDone < maxPages);

            // Basic info
            var title = playlistInfoXml.ElementStrict("title").Value;
            var author = playlistInfoXml.Element("author")?.Value ?? "";
            var description = playlistInfoXml.ElementStrict("description").Value;

            // Statistics
            var viewCount = (long) playlistInfoXml.ElementStrict("views");
            var likeCount = (long?) playlistInfoXml.Element("likes") ?? 0;
            var dislikeCount = (long?) playlistInfoXml.Element("dislikes") ?? 0;
            var statistics = new Statistics(viewCount, likeCount, dislikeCount);

            return new Playlist(playlistId, title, author, description, statistics, videos);
        }

        /// <summary>
        /// Gets playlist info by ID
        /// </summary>
        public Task<Playlist> GetPlaylistAsync(string playlistId)
            => GetPlaylistAsync(playlistId, int.MaxValue);
    }
}