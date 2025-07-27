using MediaBrowser.Model.Drawing;

namespace Emby.Subtitle.SubSource.Test
{
    public class PluginTest
    {
        private readonly Plugin _plugin;

        public PluginTest()
        {
            _plugin = new Plugin();
        }

        [Fact]
        public void Id_ShouldReturnCorrectGuid()
        {
            // Arrange
            var expectedId = new Guid("01984ab6-e3d2-7d4a-be16-6a6553c4de5c");

            // Act
            var actualId = _plugin.Id;

            // Assert
            Assert.Equal(expectedId, actualId);
        }

        [Fact]
        public void Name_ShouldReturnPluginName()
        {
            // Arrange
            var expectedName = "SubSource";

            // Act
            var actualName = _plugin.Name;

            // Assert
            Assert.Equal(expectedName, actualName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectDescription()
        {
            // Arrange
            var expectedDescription = "Download subtitles from SubSource";

            // Act
            var actualDescription = _plugin.Description;

            // Assert
            Assert.Equal(expectedDescription, actualDescription);
        }

        [Fact]
        public void ThumbImageFormat_ShouldReturnPngFormat()
        {
            // Act
            var format = _plugin.ThumbImageFormat;

            // Assert
            Assert.Equal(ImageFormat.Png, format);
        }

        [Fact]
        public void GetThumbImage_ShouldReturnValidStream()
        {
            // Act
            var stream = _plugin.GetThumbImage();

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void GetThumbImage_StreamShouldBeReadable()
        {
            // Act
            using var stream = _plugin.GetThumbImage();

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            
            // Try to read some bytes to ensure it's a valid stream
            var buffer = new byte[10];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            Assert.True(bytesRead > 0);
        }

        [Fact]
        public void Plugin_ShouldImplementIHasThumbImageInterface()
        {
            // Assert
            Assert.IsAssignableFrom<MediaBrowser.Common.Plugins.IHasThumbImage>(_plugin);
        }

        [Fact]
        public void Plugin_ShouldInheritFromBasePlugin()
        {
            // Assert
            Assert.IsAssignableFrom<MediaBrowser.Common.Plugins.BasePlugin>(_plugin);
        }

        [Fact]
        public void Id_ShouldBeConsistentAcrossMultipleCalls()
        {
            // Act
            var id1 = _plugin.Id;
            var id2 = _plugin.Id;

            // Assert
            Assert.Equal(id1, id2);
        }

        [Fact]
        public void Name_ShouldBeConsistentAcrossMultipleCalls()
        {
            // Act
            var name1 = _plugin.Name;
            var name2 = _plugin.Name;

            // Assert
            Assert.Equal(name1, name2);
        }

        [Fact]
        public void Description_ShouldBeConsistentAcrossMultipleCalls()
        {
            // Act
            var desc1 = _plugin.Description;
            var desc2 = _plugin.Description;

            // Assert
            Assert.Equal(desc1, desc2);
        }

        [Fact]
        public void GetThumbImage_ShouldReturnNewStreamOnEachCall()
        {
            // Act
            var stream1 = _plugin.GetThumbImage();
            var stream2 = _plugin.GetThumbImage();

            // Assert
            Assert.NotNull(stream1);
            Assert.NotNull(stream2);
            Assert.NotSame(stream1, stream2);
            
            // Clean up
            stream1?.Dispose();
            stream2?.Dispose();
        }
    }
}
