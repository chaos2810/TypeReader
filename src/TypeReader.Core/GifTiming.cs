namespace TypeReader.Core;

// Byte-level GIF scanner: walks the block stream, reading the delay from
// each Graphic Control Extension (0x21 0xF9) and counting image
// descriptors (0x2C) as frames. Pure data parsing — no WPF dependencies.
public static class GifTiming
{
    public static TimeSpan[] GetFrameDelays(byte[] gif)
    {
        if (gif.Length < 14 || gif[0] != (byte)'G' || gif[1] != (byte)'I' || gif[2] != (byte)'F')
            throw new ArgumentException("Not a GIF file.", nameof(gif));

        var delays = new List<TimeSpan>();
        TimeSpan pending = NormalizeDelay(0);
        bool hasGce = false;
        int i = 13; // header (6) + logical screen descriptor (7)
        if ((gif[10] & 0x80) != 0) // global color table follows the descriptor
            i += 3 * (1 << ((gif[10] & 0x07) + 1));
        while (i < gif.Length)
        {
            byte block = gif[i];
            if (block == 0x3B) break; // trailer
            if (block == 0x21 && i + 1 < gif.Length) // extension
            {
                byte label = gif[i + 1];
                i += 2;
                if (label == 0xF9 && i + 3 < gif.Length && gif[i] == 4)
                {
                    pending = NormalizeDelay(gif[i + 2] | (gif[i + 3] << 8));
                    hasGce = true;
                }
                i = SkipSubBlocks(gif, i);
            }
            else if (block == 0x2C) // image descriptor = one frame
            {
                delays.Add(hasGce ? pending : NormalizeDelay(0));
                hasGce = false;
                pending = NormalizeDelay(0);
                i += 10; // introducer (1) + descriptor (9); gif[i-1] is the packed byte
                if (i < gif.Length && (gif[i - 1] & 0x80) != 0)
                    i += 3 * (1 << ((gif[i - 1] & 0x07) + 1)); // local color table
                i = SkipSubBlocks(gif, i + 1); // +1 past the LZW min code size byte
            }
            else break;
        }
        return delays.ToArray();
    }

    // GIF delays are stored in hundredths of a second. Browsers treat
    // missing/zero as ~100ms and clamp tiny values to >= 20ms; we do the same.
    public static TimeSpan NormalizeDelay(int hundredths)
    {
        if (hundredths <= 1) return TimeSpan.FromMilliseconds(100);
        return TimeSpan.FromMilliseconds(Math.Max(hundredths * 10, 20));
    }

    private static int SkipSubBlocks(byte[] gif, int i)
    {
        while (i < gif.Length && gif[i] != 0)
            i += 1 + gif[i];
        return i + 1; // past the 0x00 terminator
    }
}
