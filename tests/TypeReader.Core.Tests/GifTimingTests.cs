using TypeReader.Core;

namespace TypeReader.Core.Tests;

public class GifTimingTests
{
    // Minimal GIF skeleton: 6-byte header + 7-byte screen descriptor,
    // then optional GCE + image descriptor per frame, then trailer.
    private static byte[] BuildGif(params (int delay, bool gce)[] frames)
    {
        var bytes = new List<byte>();
        bytes.AddRange("GIF89a"u8.ToArray());
        bytes.AddRange(new byte[7]);
        foreach (var (delay, gce) in frames)
        {
            if (gce)
            {
                bytes.AddRange(new byte[] { 0x21, 0xF9, 0x04, 0x00,
                    (byte)(delay & 0xFF), (byte)(delay >> 8), 0x00, 0x00 });
            }
            bytes.AddRange(new byte[] { 0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0x00 });
            bytes.Add(0x02);
            bytes.AddRange(new byte[] { 0x01, 0x2C, 0x00 });
        }
        bytes.Add(0x3B);
        return bytes.ToArray();
    }

    [Fact]
    public void SingleFrame_ReadsDelay()
    {
        var delays = GifTiming.GetFrameDelays(BuildGif((5, true)));
        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromMilliseconds(50), delays[0]);
    }

    [Fact]
    public void MultiFrame_KeepsOrder()
    {
        var delays = GifTiming.GetFrameDelays(BuildGif((3, true), (0, true)));
        Assert.Equal(2, delays.Length);
        Assert.Equal(TimeSpan.FromMilliseconds(30), delays[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), delays[1]);
    }

    [Fact]
    public void FrameWithoutGce_UsesDefault()
    {
        var delays = GifTiming.GetFrameDelays(BuildGif((5, true), (0, false)));
        Assert.Equal(TimeSpan.FromMilliseconds(50), delays[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), delays[1]);
    }

    [Fact]
    public void NoFrames_ReturnsEmpty()
    {
        Assert.Empty(GifTiming.GetFrameDelays(BuildGif()));
    }

    [Fact]
    public void NotAGif_Throws()
    {
        Assert.Throws<ArgumentException>(() => GifTiming.GetFrameDelays(new byte[100]));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 100)]
    [InlineData(2, 20)]
    [InlineData(7, 70)]
    [InlineData(10, 100)]
    public void NormalizeDelay_AppliesDefaultAndMinimum(int raw, int expectedMs)
    {
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), GifTiming.NormalizeDelay(raw));
    }
}
