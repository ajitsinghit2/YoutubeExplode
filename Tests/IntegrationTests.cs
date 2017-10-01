using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Models.MediaStreams;

namespace Tests
{
    [TestClass]
    public partial class IntegrationTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds.csv",
            "Integration_VideoIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_CheckVideoExistsAsync_Existing_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var exists = await client.CheckVideoExistsAsync(id);

            Assert.IsTrue(exists);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds_NonExisting.csv",
            "Integration_VideoIds_NonExisting#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_CheckVideoExistsAsync_NonExisting_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var exists = await client.CheckVideoExistsAsync(id);

            Assert.IsFalse(exists);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds.csv",
            "Integration_VideoIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetVideoInfoAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(id);

            Assert.AreEqual(id, videoInfo.Id);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds_NonExisting.csv",
            "Integration_VideoIds_NonExisting#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetVideoInfoAsync_NonExisting_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            await Assert.ThrowsExceptionAsync<VideoNotAvailableException>(() => client.GetVideoAsync(id));
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds_Paid.csv",
            "Integration_VideoIds_Paid#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetVideoInfoAsync_Paid_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            await Assert.ThrowsExceptionAsync<VideoRequiresPurchaseException>(() => client.GetVideoAsync(id));
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_PlaylistIds.csv",
            "Integration_PlaylistIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetPlaylistInfoAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];
            var minVideoCount = (int) TestContext.DataRow["MinVideoCount"];

            var client = new YoutubeClient();
            var playlistInfo = await client.GetPlaylistAsync(id);

            Assert.AreEqual(id, playlistInfo.Id);
            Assert.IsTrue(minVideoCount <= playlistInfo.Videos.Count);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_PlaylistIds.csv",
            "Integration_PlaylistIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetPlaylistInfoAsync_Truncated_Test()
        {
            var id = (string) TestContext.DataRow["Id"];
            var pageLimit = 1;

            var client = new YoutubeClient();
            var playlistInfo = await client.GetPlaylistAsync(id, pageLimit);

            Assert.AreEqual(id, playlistInfo.Id);
            Assert.IsTrue(200 * pageLimit >= playlistInfo.Videos.Count);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_ChannelIds.csv",
            "Integration_ChannelIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetChannelUploadsAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var videos = await client.GetChannelUploadsAsync(id);

            Assert.IsNotNull(videos);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_ChannelIds.csv",
            "Integration_ChannelIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetChannelInfoAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var channelInfo = await client.GetChannelAsync(id);

            Assert.IsNotNull(channelInfo);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds.csv",
            "Integration_VideoIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetMediaStreamAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(id);

            var streams = new List<MediaStreamInfo>();
            streams.AddRange(videoInfo.MuxedStreams);
            streams.AddRange(videoInfo.AudioStreams);
            streams.AddRange(videoInfo.VideoStreams);

            foreach (var streamInfo in streams)
            {
                using (var stream = await client.GetMediaStreamAsync(streamInfo))
                {
                    Assert.IsNotNull(stream);

                    var buffer = new byte[100];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                }
            }
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds_HasCC.csv",
            "Integration_VideoIds_HasCC#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_GetClosedCaptionTrackAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(id);

            var trackInfo = videoInfo.ClosedCaptionTracks.First();
            var track = await client.GetClosedCaptionTrackAsync(trackInfo);

            Assert.IsNotNull(track);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds.csv",
            "Integration_VideoIds#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_DownloadMediaStreamAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(id);

            var streamInfo = videoInfo.AudioStreams.OrderBy(s => s.ContentLength).First();
            var outputFilePath = Path.Combine(Shared.TempDirectoryPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(Shared.TempDirectoryPath);
            await client.DownloadMediaStreamAsync(streamInfo, outputFilePath);

            var fileInfo = new FileInfo(outputFilePath);
            Assert.IsTrue(fileInfo.Exists);
            Assert.IsTrue(0 < fileInfo.Length);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV", "Data\\Integration_VideoIds_HasCC.csv",
            "Integration_VideoIds_HasCC#csv", DataAccessMethod.Sequential)]
        public async Task YoutubeClient_DownloadClosedCaptionTrackAsync_Test()
        {
            var id = (string) TestContext.DataRow["Id"];

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(id);

            var streamInfo = videoInfo.ClosedCaptionTracks.First();
            var outputFilePath = Path.Combine(Shared.TempDirectoryPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(Shared.TempDirectoryPath);
            await client.DownloadClosedCaptionTrackAsync(streamInfo, outputFilePath);

            var fileInfo = new FileInfo(outputFilePath);
            Assert.IsTrue(fileInfo.Exists);
            Assert.IsTrue(0 < fileInfo.Length);
        }
    }

    public partial class IntegrationTests
    {
        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (Directory.Exists(Shared.TempDirectoryPath))
                Directory.Delete(Shared.TempDirectoryPath, true);
        }
    }
}