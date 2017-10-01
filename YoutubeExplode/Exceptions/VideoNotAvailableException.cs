using System;

namespace YoutubeExplode.Exceptions
{
    /// <summary>
    /// Thrown when video is not available
    /// </summary>
    public class VideoNotAvailableException : Exception
    {
        /// <summary>
        /// Error code
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// Error reason
        /// </summary>
        public string Reason { get; }

        /// <inheritdoc />
        public VideoNotAvailableException(int code, string reason)
            : base("The video is not available")
        {
            Code = code;
            Reason = reason;
        }
    }
}