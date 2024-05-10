namespace Toimik.WarcProtocol.Tests;

using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class LineReaderTest
{
    [Fact]
    public async Task OffsetNaively()
    {
        var streamMock = new Mock<Stream>();
        streamMock.Setup(s => s.CanSeek)
            .Returns(false);
        streamMock.SetupSequence(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .ReturnsAsync(0);

        var lineReader = new LineReader(streamMock.Object, CancellationToken.None);
        await lineReader.Offset(2);
        return;
    }
}