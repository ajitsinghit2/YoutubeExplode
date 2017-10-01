﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tyrrrz.Extensions;
using YoutubeExplode;
using YoutubeExplode.Models;

namespace DemoConsole
{
    public static class Program
    {
        /// <summary>
        /// If given a youtube url, parses video id from it.
        /// Otherwise returns the same string.
        /// </summary>
        private static string NormalizeId(string input)
        {
            if (!YoutubeClient.TryParseVideoId(input, out string id))
                id = input;
            return id;
        }

        /// <summary>
        /// Turns file size in bytes into human-readable string
        /// </summary>
        private static string NormalizeFileSize(long fileSize)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            double size = fileSize;
            var unit = 0;

            while (size >= 1024)
            {
                size /= 1024;
                ++unit;
            }

            return $"{size:0.#} {units[unit]}";
        }

        private static async Task MainAsync()
        {
            // Client
            var client = new YoutubeClient();

            // Get the video ID
            Console.Write("Enter Youtube video ID or URL: ");
            var id = Console.ReadLine();
            id = NormalizeId(id);

            // Get the video info
            Console.WriteLine("Loading...");
            var video = await client.GetVideoAsync(id);
            Console.WriteLine('-'.Repeat(100));

            // Print metadata
            Console.WriteLine($"Id: {video.Id} | Title: {video.Title} | Author: {video.Author.Title}");

            // Get the most preferable stream
            Console.WriteLine("Looking for the best mixed stream...");
            var streamInfo = video.MuxedStreams
                .OrderBy(s => s.VideoQuality)
                .Last();
            var normalizedFileSize = NormalizeFileSize(streamInfo.ContentLength);
            Console.WriteLine($"Quality: {streamInfo.VideoQualityLabel} | Container: {streamInfo.Container} | Size: {normalizedFileSize}");

            // Compose file name, based on metadata
            var fileExtension = streamInfo.Container.GetFileExtension();
            var fileName = $"{video.Title}.{fileExtension}";

            // Remove illegal characters from file name
            fileName = fileName.Except(Path.GetInvalidFileNameChars());

            // Download video
            Console.WriteLine($"Downloading to [{fileName}]...");
            Console.WriteLine('-'.Repeat(100));

            var progress = new Progress<double>(p => Console.Title = $"YoutubeExplode Demo [{p:P0}]");
            await client.DownloadMediaStreamAsync(streamInfo, fileName, progress);

            Console.WriteLine("Download complete!");
            Console.ReadKey();
        }

        public static void Main(string[] args)
        {
            // This demo prompts for video ID, gets video info and downloads one media stream
            // It's intended to be very simple and straight to the point
            // For a more complicated example - check out the WPF demo

            Console.Title = "YoutubeExplode Demo";

            // Main method in consoles cannot be asynchronous so we run everything synchronously
            MainAsync().GetAwaiter().GetResult();
        }
    }
}
