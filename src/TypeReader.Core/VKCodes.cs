namespace TypeReader.Core;

public static class VKCodes
{
    public const int SHIFT = 0x10;
    public const int CONTROL = 0x11;
    public const int ALT = 0x12;
    public const int LWIN = 0x5B;
    public const int RWIN = 0x5C;

    public static bool IsModifier(int vk)
    {
        return vk is >= 0x10 and <= 0x12 or 0x5B or 0x5C;
    }
}