﻿using System;

namespace YoutubeExplode.Exceptions
{
    /// <summary>
    /// Thrown when there was an error parsing a response
    /// </summary>
    public class ParseException : Exception
    {
        /// <inheritdoc />
        public ParseException(string message)
            : base(message)
        {
        }
    }
}