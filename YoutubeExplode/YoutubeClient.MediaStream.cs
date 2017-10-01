using System;
using System.Threading.Tasks;
using YoutubeExplode.Models.MediaStreams;
using YoutubeExplode.Services;

#if NET45 || NETCOREAPP1_0
using System.IO;
using System.Threading;
#endif

namespace YoutubeExplode
{
    public partial class YoutubeClient
    {
        /// <summary>
        /// Gets the actual media stream represented by given metadata
        /// </summary>
        public async Task<MediaStream> GetMediaStreamAsync(MediaStreamInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            // Get
            var stream = await _httpService.GetStreamAsync(info.Url).ConfigureAwait(false);

            return new MediaStream(info, stream);
        }

#if NET45 || NETCOREAPP1_0

        /// <summary>
        /// Downloads a media stream to file
        /// </summary>
        public async Task DownloadMediaStreamAsync(MediaStreamInfo info, string filePath,
            IProgress<double> progress, CancellationToken cancellationToken, int bufferSize)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            // Save to file
            using (var input = await GetMediaStreamAsync(info).ConfigureAwait(false))
            using (var output = File.Create(filePath, bufferSize))
            {
                var buffer = new byte[bufferSize];
                int bytesRead;
                long totalBytesRead = 0;
                do
                {
                    // Read
                    totalBytesRead += bytesRead =
                        await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                    // Write
                    await output.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                    // Report progress
                    progress?.Report(1.0 * totalBytesRead / input.Length);
                } while (bytesRead > 0);
            }
        }

        /// <summary>
        /// Downloads a media stream to file
        /// </summary>
        public Task DownloadMediaStreamAsync(MediaStreamInfo info, string filePath, IProgress<double> progress,
            CancellationToken cancellationToken)
            => DownloadMediaStreamAsync(info, filePath, progress, cancellationToken, 4096);

        /// <summary>
        /// Downloads a media stream to file
        /// </summary>
        public Task DownloadMediaStreamAsync(MediaStreamInfo info, string filePath, IProgress<double> progress)
            => DownloadMediaStreamAsync(info, filePath, progress, CancellationToken.None);

        /// <summary>
        /// Downloads a media stream to file
        /// </summary>
        public Task DownloadMediaStreamAsync(MediaStreamInfo info, string filePath)
            => DownloadMediaStreamAsync(info, filePath, null);

#endif
    }
}