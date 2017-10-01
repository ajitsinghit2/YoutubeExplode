using System;
using System.Collections.Generic;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace YoutubeExplode.Models
{
    /// <summary>
    /// Video
    /// </summary>
    public class Video
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Author channel
        /// </summary>
        public Channel Author { get; }

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
        /// Status
        /// </summary>
        public VideoStatus Status { get; }

        /// <summary>
        /// Statistics
        /// </summary>
        public Statistics Statistics { get; }

        /// <summary>
        /// Muxed streams available for this video
        /// </summary>
        public IReadOnlyList<MuxedStreamInfo> MuxedStreams { get; }

        /// <summary>
        /// Audio-only streams available for this video
        /// </summary>
        public IReadOnlyList<AudioStreamInfo> AudioStreams { get; }

        /// <summary>
        /// Video-only streams available for this video
        /// </summary>
        public IReadOnlyList<VideoStreamInfo> VideoStreams { get; }

        /// <summary>
        /// Closed caption tracks available for this video
        /// </summary>
        public IReadOnlyList<ClosedCaptionTrackInfo> ClosedCaptionTracks { get; }

        /// <inheritdoc />
        public Video(string id, Channel author, string title, string description, VideoThumbnails thumbnails,
            TimeSpan duration, IReadOnlyList<string> keywords, VideoStatus status, Statistics statistics,
            IReadOnlyList<MuxedStreamInfo> muxedStreams, IReadOnlyList<AudioStreamInfo> audioStreams,
            IReadOnlyList<VideoStreamInfo> videoStreams, IReadOnlyList<ClosedCaptionTrackInfo> closedCaptionTracks)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration));

            Id = id ?? throw new ArgumentNullException(nameof(id));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
            Duration = duration;
            Keywords = keywords ?? throw new ArgumentNullException(nameof(keywords));
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            MuxedStreams = muxedStreams ?? throw new ArgumentNullException(nameof(muxedStreams));
            AudioStreams = audioStreams ?? throw new ArgumentNullException(nameof(audioStreams));
            VideoStreams = videoStreams ?? throw new ArgumentNullException(nameof(videoStreams));
            ClosedCaptionTracks = closedCaptionTracks ?? throw new ArgumentNullException(nameof(closedCaptionTracks));
        }
    }
}