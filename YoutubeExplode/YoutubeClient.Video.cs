using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Internal;
using YoutubeExplode.Internal.CipherOperations;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;
using YoutubeExplode.Services;

namespace YoutubeExplode
{
    public partial class YoutubeClient
    {
        private async Task<PlayerContext> GetPlayerContextAsync(string videoId)
        {
            // Get the embed video page
            var request = YoutubeHost + $"/embed/{videoId}";
            var response = await _httpService.GetStringAsync(request).ConfigureAwait(false);

            // Extract values
            var sourceUrl = Regex.Match(response, @"""js""\s*:\s*""(.*?)""").Groups[1].Value.Replace("\\", "");
            var sts = Regex.Match(response, @"""sts""\s*:\s*(\d+)").Groups[1].Value;

            // Check if successful
            if (sourceUrl.IsBlank() || sts.IsBlank())
                throw new ParseException("Could not parse player context");

            // Append host to source url
            sourceUrl = YoutubeHost + sourceUrl;

            return new PlayerContext(sourceUrl, sts);
        }

        private async Task<PlayerSource> GetPlayerSourceAsync(string sourceUrl)
        {
            // Original code credit: Decipherer class of https://github.com/flagbug/YoutubeExtractor

            // Try to resolve from cache first
            var playerSource = _playerSourceCache.GetOrDefault(sourceUrl);
            if (playerSource != null)
                return playerSource;

            // Get player source code
            var response = await _httpService.GetStringAsync(sourceUrl).ConfigureAwait(false);

            // Find the name of the function that handles deciphering
            var funcName = Regex.Match(response, @"\""signature"",\s?([a-zA-Z0-9\$]+)\(").Groups[1].Value;
            if (funcName.IsBlank())
                throw new ParseException("Could not find the entry function for signature deciphering");

            // Find the body of the function
            var funcPattern = @"(?!h\.)" + Regex.Escape(funcName) + @"=function\(\w+\)\{(.*?)\}";
            var funcBody = Regex.Match(response, funcPattern, RegexOptions.Singleline).Groups[1].Value;
            if (funcBody.IsBlank())
                throw new ParseException("Could not find the signature decipherer function body");
            var funcLines = funcBody.Split(";").ToArray();

            // Identify cipher functions
            string reverseFuncName = null;
            string sliceFuncName = null;
            string charSwapFuncName = null;
            var operations = new List<ICipherOperation>();

            // Analyze the function body to determine the names of cipher functions
            foreach (var line in funcLines)
            {
                // Break when all functions are found
                if (reverseFuncName.IsNotBlank() && sliceFuncName.IsNotBlank() && charSwapFuncName.IsNotBlank())
                    break;

                // Get the function called on this line
                var calledFunctionName = Regex.Match(line, @"\w+\.(\w+)\(").Groups[1].Value;
                if (calledFunctionName.IsBlank())
                    continue;

                // Find cipher function names
                if (Regex.IsMatch(response, $@"{Regex.Escape(calledFunctionName)}:\bfunction\b\(\w+\)"))
                {
                    reverseFuncName = calledFunctionName;
                }
                else if (Regex.IsMatch(response,
                    $@"{Regex.Escape(calledFunctionName)}:\bfunction\b\([a],b\).(\breturn\b)?.?\w+\."))
                {
                    sliceFuncName = calledFunctionName;
                }
                else if (Regex.IsMatch(response,
                    $@"{Regex.Escape(calledFunctionName)}:\bfunction\b\(\w+\,\w\).\bvar\b.\bc=a\b"))
                {
                    charSwapFuncName = calledFunctionName;
                }
            }

            // Analyze the function body again to determine the operation set and order
            foreach (var line in funcLines)
            {
                // Get the function called on this line
                var calledFunctionName = Regex.Match(line, @"\w+\.(\w+)\(").Groups[1].Value;
                if (calledFunctionName.IsBlank())
                    continue;

                // Swap operation
                if (calledFunctionName == charSwapFuncName)
                {
                    var index = Regex.Match(line, @"\(\w+,(\d+)\)").Groups[1].Value.ParseInt();
                    operations.Add(new SwapCipherOperation(index));
                }
                // Slice operation
                else if (calledFunctionName == sliceFuncName)
                {
                    var index = Regex.Match(line, @"\(\w+,(\d+)\)").Groups[1].Value.ParseInt();
                    operations.Add(new SliceCipherOperation(index));
                }
                // Reverse operation
                else if (calledFunctionName == reverseFuncName)
                {
                    operations.Add(new ReverseCipherOperation());
                }
            }

            return _playerSourceCache[sourceUrl] = new PlayerSource(operations);
        }

        /// <summary>
        /// Checks whether a video with the given ID exists
        /// </summary>
        public async Task<bool> CheckVideoExistsAsync(string videoId)
        {
            if (videoId == null)
                throw new ArgumentNullException(nameof(videoId));
            if (!ValidateVideoId(videoId))
                throw new ArgumentException("Invalid Youtube video ID", nameof(videoId));

            // Get
            var request = YoutubeHost + $"/get_video_info?video_id={videoId}&el=info&ps=default&hl=en";
            var response = await _httpService.GetStringAsync(request).ConfigureAwait(false);

            // Parse
            var videoInfoDic = UrlHelper.GetDictionaryFromUrlQuery(response);

            // Check error code
            if (videoInfoDic.ContainsKey("errorcode"))
            {
                var errorCode = videoInfoDic.Get("errorcode").ParseInt();
                return errorCode != 100 && errorCode != 150;
            }

            return true;
        }

        /// <summary>
        /// Gets video info by ID
        /// </summary>
        public async Task<Video> GetVideoAsync(string videoId)
        {
            if (videoId == null)
                throw new ArgumentNullException(nameof(videoId));
            if (!ValidateVideoId(videoId))
                throw new ArgumentException("Invalid Youtube video ID", nameof(videoId));

            // Get player context
            var context = await GetPlayerContextAsync(videoId).ConfigureAwait(false);

            // Get video info
            var request = YoutubeHost +
                          $"/get_video_info?video_id={videoId}&sts={context.Sts}&el=info&ps=default&hl=en";
            var response = await _httpService.GetStringAsync(request).ConfigureAwait(false);
            var videoInfoDic = UrlHelper.GetDictionaryFromUrlQuery(response);

            // Check for error
            if (videoInfoDic.ContainsKey("errorcode"))
            {
                var errorCode = videoInfoDic.Get("errorcode").ParseInt();
                var errorReason = videoInfoDic.Get("reason");
                throw new VideoNotAvailableException(errorCode, errorReason);
            }

            // Check for paid content
            if (videoInfoDic.GetOrDefault("requires_purchase") == "1")
            {
                var previewVideoId = videoInfoDic.Get("ypc_vid");
                throw new VideoRequiresPurchaseException(previewVideoId);
            }

            // Parse metadata
            var title = videoInfoDic.Get("title");
            var duration = TimeSpan.FromSeconds(videoInfoDic.Get("length_seconds").ParseDouble());
            var viewCount = videoInfoDic.Get("view_count").ParseLong();
            var keywords = videoInfoDic.Get("keywords").Split(",");
            var isListed = videoInfoDic.GetOrDefault("is_listed") == "1";
            var isRatingAllowed = videoInfoDic.Get("allow_ratings") == "1";
            var isMuted = videoInfoDic.Get("muted") == "1";
            var isEmbeddingAllowed = videoInfoDic.Get("allow_embed") == "1";

            // Parse mixed streams
            var muxedStreams = new List<MuxedStreamInfo>();
            var muxedStreamsEncoded = videoInfoDic.GetOrDefault("url_encoded_fmt_stream_map");
            if (muxedStreamsEncoded.IsNotBlank())
            {
                foreach (var streamEncoded in muxedStreamsEncoded.Split(","))
                {
                    var streamInfoDic = UrlHelper.GetDictionaryFromUrlQuery(streamEncoded);

                    var itag = streamInfoDic.Get("itag").ParseInt();

#if RELEASE
                    // Skip unknown itags on RELEASE
                    if (!MediaStreamInfo.IsKnown(itag)) continue;
#endif

                    var url = streamInfoDic.Get("url");
                    var sig = streamInfoDic.GetOrDefault("s");

                    // Decipher signature if needed
                    if (sig.IsNotBlank())
                    {
                        var playerSource = await GetPlayerSourceAsync(context.SourceUrl).ConfigureAwait(false);
                        sig = playerSource.Decipher(sig);
                        url = UrlHelper.SetUrlQueryParameter(url, "signature", sig);
                    }

                    // Get content length
                    long contentLength;
                    using (var reqMsg = new HttpRequestMessage(HttpMethod.Head, url))
                    using (var resMsg = await _httpService.PerformRequestAsync(reqMsg).ConfigureAwait(false))
                    {
                        // Check status code (https://github.com/Tyrrrz/YoutubeExplode/issues/36)
                        if (resMsg.StatusCode == HttpStatusCode.NotFound ||
                            resMsg.StatusCode == HttpStatusCode.Gone)
                            continue;

                        // Ensure success
                        resMsg.EnsureSuccessStatusCode();

                        // Extract content length
                        contentLength = resMsg.Content.Headers.ContentLength ?? -1;
                        if (contentLength < 0)
                            throw new ParseException("Could not extract content length");
                    }

                    // Set rate bypass
                    url = UrlHelper.SetUrlQueryParameter(url, "ratebypass", "yes");

                    var stream = new MuxedStreamInfo(itag, url, contentLength);
                    muxedStreams.Add(stream);
                }
            }

            // Parse adaptive streams
            var audioStreams = new List<AudioStreamInfo>();
            var videoStreams = new List<VideoStreamInfo>();
            var adaptiveStreamsEncoded = videoInfoDic.GetOrDefault("adaptive_fmts");
            if (adaptiveStreamsEncoded.IsNotBlank())
            {
                foreach (var streamEncoded in adaptiveStreamsEncoded.Split(","))
                {
                    var streamInfoDic = UrlHelper.GetDictionaryFromUrlQuery(streamEncoded);

                    var itag = streamInfoDic.Get("itag").ParseInt();

#if RELEASE
                    // Skip unknown itags on RELEASE
                    if (!MediaStreamInfo.IsKnown(itag)) continue;
#endif

                    var url = streamInfoDic.Get("url");
                    var sig = streamInfoDic.GetOrDefault("s");
                    var contentLength = streamInfoDic.Get("clen").ParseLong();
                    var bitrate = streamInfoDic.Get("bitrate").ParseLong();

                    // Decipher signature if needed
                    if (sig.IsNotBlank())
                    {
                        var playerSource = await GetPlayerSourceAsync(context.SourceUrl).ConfigureAwait(false);
                        sig = playerSource.Decipher(sig);
                        url = UrlHelper.SetUrlQueryParameter(url, "signature", sig);
                    }

                    // Set rate bypass
                    url = UrlHelper.SetUrlQueryParameter(url, "ratebypass", "yes");

                    // Check if audio
                    var isAudio = streamInfoDic.Get("type").Contains("audio/");

                    // If audio stream
                    if (isAudio)
                    {
                        var stream = new AudioStreamInfo(itag, url, contentLength, bitrate);
                        audioStreams.Add(stream);
                    }
                    // If video stream
                    else
                    {
                        // Parse additional data
                        var size = streamInfoDic.Get("size");
                        var width = size.SubstringUntil("x").ParseInt();
                        var height = size.SubstringAfter("x").ParseInt();
                        var resolution = new VideoResolution(width, height);
                        var framerate = streamInfoDic.Get("fps").ParseDouble();

                        var stream = new VideoStreamInfo(itag, url, contentLength, bitrate, resolution, framerate);
                        videoStreams.Add(stream);
                    }
                }
            }

            // Parse adaptive streams from dash
            var dashManifestUrl = videoInfoDic.GetOrDefault("dashmpd");
            if (dashManifestUrl.IsNotBlank())
            {
                // Parse signature
                var sig = Regex.Match(dashManifestUrl, @"/s/(.*?)(?:/|$)").Groups[1].Value;

                // Decipher signature if needed
                if (sig.IsNotBlank())
                {
                    var playerSource = await GetPlayerSourceAsync(context.SourceUrl).ConfigureAwait(false);
                    sig = playerSource.Decipher(sig);
                    dashManifestUrl = UrlHelper.SetUrlPathParameter(dashManifestUrl, "signature", sig);
                }

                // Get the manifest
                response = await _httpService.GetStringAsync(dashManifestUrl).ConfigureAwait(false);
                var dashManifestXml = XElement.Parse(response).StripNamespaces();
                var streamsXml = dashManifestXml.Descendants("Representation");

                // Filter out partial streams
                streamsXml = streamsXml
                    .Where(x => !(x.Descendant("Initialization")
                                      ?.Attribute("sourceURL")
                                      ?.Value.Contains("sq/") ?? false));

                // Parse streams
                foreach (var streamXml in streamsXml)
                {
                    var itag = (int) streamXml.AttributeStrict("id");

#if RELEASE
                    // Skip unknown itags on RELEASE
                    if (!MediaStreamInfo.IsKnown(itag)) continue;
#endif

                    var url = streamXml.ElementStrict("BaseURL").Value;
                    var bitrate = (long) streamXml.AttributeStrict("bandwidth");

                    // Parse content length
                    var contentLength = Regex.Match(url, @"clen[/=](\d+)").Groups[1].Value.ParseLong();

                    // Set rate bypass
                    url = url.Contains("?")
                        ? UrlHelper.SetUrlQueryParameter(url, "ratebypass", "yes")
                        : UrlHelper.SetUrlPathParameter(url, "ratebypass", "yes");

                    // Check if audio stream
                    var isAudio = streamXml.Element("AudioChannelConfiguration") != null;

                    // If audio stream
                    if (isAudio)
                    {
                        var stream = new AudioStreamInfo(itag, url, contentLength, bitrate);
                        audioStreams.Add(stream);
                    }
                    // If video stream
                    else
                    {
                        // Parse additional data
                        var width = (int) streamXml.AttributeStrict("width");
                        var height = (int) streamXml.AttributeStrict("height");
                        var resolution = new VideoResolution(width, height);
                        var framerate = (double) streamXml.AttributeStrict("frameRate");

                        var stream = new VideoStreamInfo(itag, url, contentLength, bitrate, resolution, framerate);
                        videoStreams.Add(stream);
                    }
                }
            }

            // Finalize stream lists
            muxedStreams = muxedStreams.Distinct(s => s.Itag).OrderByDescending(s => s.VideoQuality).ToList();
            audioStreams = audioStreams.Distinct(s => s.Itag).OrderByDescending(s => s.Bitrate).ToList();
            videoStreams = videoStreams.Distinct(s => s.Itag).OrderByDescending(s => s.VideoQuality).ToList();

            // Parse closed caption tracks
            var captionTracks = new List<ClosedCaptionTrackInfo>();
            var captionTracksEncoded = videoInfoDic.GetOrDefault("caption_tracks");
            if (captionTracksEncoded.IsNotBlank())
            {
                foreach (var captionEncoded in captionTracksEncoded.Split(","))
                {
                    var captionInfoDic = UrlHelper.GetDictionaryFromUrlQuery(captionEncoded);

                    var url = captionInfoDic.Get("u");

                    var code = captionInfoDic.Get("lc");
                    var name = captionInfoDic.Get("n");
                    var language = new Language(code, name);

                    var isAuto = captionInfoDic.Get("v").Contains("a.");

                    var captionTrack = new ClosedCaptionTrackInfo(url, language, isAuto);
                    captionTracks.Add(captionTrack);
                }
            }

            // Get metadata extension
            request = YoutubeHost + $"/get_video_metadata?video_id={videoId}";
            response = await _httpService.GetStringAsync(request).ConfigureAwait(false);
            var videoInfoExtXml = XElement.Parse(response).StripNamespaces().ElementStrict("html_content");

            // Parse
            var description = videoInfoExtXml.ElementStrict("video_info").ElementStrict("description").Value;
            var likeCount = (long) videoInfoExtXml.ElementStrict("video_info").ElementStrict("likes_count_unformatted");
            var dislikeCount = (long) videoInfoExtXml.ElementStrict("video_info")
                .ElementStrict("dislikes_count_unformatted");

            // Parse author info
            var authorId = videoInfoExtXml.ElementStrict("user_info").ElementStrict("channel_external_id").Value;
            var authorName = videoInfoExtXml.ElementStrict("user_info").ElementStrict("username").Value;
            var authorTitle = videoInfoExtXml.ElementStrict("user_info").ElementStrict("channel_title").Value;
            var authorIsPaid = videoInfoExtXml.ElementStrict("user_info").ElementStrict("channel_paid").Value == "1";
            var authorLogoUrl = videoInfoExtXml.ElementStrict("user_info").ElementStrict("channel_logo_url").Value;
            var authorBannerUrl = videoInfoExtXml.ElementStrict("user_info").ElementStrict("channel_banner_url").Value;
            var author = new Channel(
                authorId, authorName, authorTitle,
                authorIsPaid, authorLogoUrl, authorBannerUrl);

            var status = new VideoStatus(isListed, isRatingAllowed, isMuted, isEmbeddingAllowed);
            var thumbnails = new VideoThumbnails(videoId);
            var statistics = new Statistics(viewCount, likeCount, dislikeCount);
            return new Video(videoId, author, title, description, thumbnails, duration, keywords, status, statistics,
                muxedStreams, audioStreams, videoStreams, captionTracks);
        }
    }
}