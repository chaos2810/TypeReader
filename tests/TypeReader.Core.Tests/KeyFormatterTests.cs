using TypeReader.Core;

namespace TypeReader.Core.Tests;

public class KeyFormatterTests
{
    private static bool[] State(params int[] pressed)
    {
        var state = new bool[256];
        foreach (var vk in pressed) state[vk] = true;
        return state;
    }

    [Fact]
    public void PlainLetter_KeyDown_ReturnsCharacter()
    {
        var f = new KeyFormatter();
        var result = f.Format(0x41, State(), isKeyDown: true); // 'A' key
        Assert.Equal("a", result);
    }

    [Fact]
    public void ShiftLetter_KeyDown_ReturnsShiftedCharacter()
    {
        var f = new KeyFormatter();
        var result = f.Format(0x41, State(VKCodes.SHIFT), isKeyDown: true);
        Assert.Equal("A", result);
    }

    [Fact]
    public void CtrlS_KeyDown_ReturnsCombo()
    {
        var f = new KeyFormatter();
        f.Format(VKCodes.CONTROL, State(VKCodes.CONTROL), isKeyDown: true);
        var result = f.Format(0x53, State(VKCodes.CONTROL), isKeyDown: true); // 'S' key
        Assert.Equal("Ctrl + S", result);
    }

    [Fact]
    public void CtrlShiftT_KeyDown_ReturnsCombo()
    {
        var f = new KeyFormatter();
        f.Format(VKCodes.CONTROL, State(VKCodes.CONTROL), isKeyDown: true);
        f.Format(VKCodes.SHIFT, State(VKCodes.CONTROL, VKCodes.SHIFT), isKeyDown: true);
        var result = f.Format(0x54, State(VKCodes.CONTROL, VKCodes.SHIFT), isKeyDown: true); // 'T' key
        Assert.Equal("Ctrl + Shift + T", result);
    }

    [Fact]
    public void CtrlAlone_ShowsModifierName()
    {
        var f = new KeyFormatter();
        var result = f.Format(VKCodes.CONTROL, State(VKCodes.CONTROL), isKeyDown: true);
        Assert.Equal("Ctrl", result);
    }

    [Fact]
    public void CtrlReleased_AfterCtrlAlone_NoNewDisplay()
    {
        var f = new KeyFormatter();
        f.Format(VKCodes.CONTROL, State(VKCodes.CONTROL), isKeyDown: true);
        var result = f.Format(VKCodes.CONTROL, State(), isKeyDown: false);
        Assert.Null(result); // display unchanged (last modifier still shown)
    }

    [Fact]
    public void FunctionKey_ShowsFriendlyName()
    {
        var f = new KeyFormatter();
        Assert.Equal("F1", f.Format(0x70, State(), isKeyDown: true));
        Assert.Equal("F12", f.Format(0x7B, State(), isKeyDown: true));
    }

    [Fact]
    public void Enter_ShowsFriendlyName()
    {
        var f = new KeyFormatter();
        Assert.Equal("Enter", f.Format(0x0D, State(), isKeyDown: true));
    }

    [Fact]
    public void Esc_ShowsFriendlyName()
    {
        var f = new KeyFormatter();
        Assert.Equal("Esc", f.Format(0x1B, State(), isKeyDown: true));
    }

    [Fact]
    public void Space_ShowsFriendlyName()
    {
        var f = new KeyFormatter();
        Assert.Equal("Space", f.Format(0x20, State(), isKeyDown: true));
    }

    [Fact]
    public void LeftArrow_ShowsFriendlyName()
    {
        var f = new KeyFormatter();
        Assert.Equal("←", f.Format(0x25, State(), isKeyDown: true));
        Assert.Equal("↑", f.Format(0x26, State(), isKeyDown: true));
    }

    [Fact]
    public void NumberRow_Shifted_ReturnsSymbol()
    {
        var f = new KeyFormatter();
        var result = f.Format(0x31, State(VKCodes.SHIFT), isKeyDown: true); // '1' key
        Assert.Equal("!", result);
    }

    [Fact]
    public void KeyUp_NonModifier_ReturnsNull()
    {
        var f = new KeyFormatter();
        Assert.Null(f.Format(0x41, State(), isKeyDown: false));
    }

    [Fact]
    public void WinKey_ShowsModifierName()
    {
        var f = new KeyFormatter();
        Assert.Equal("Win", f.Format(VKCodes.LWIN, State(VKCodes.LWIN), isKeyDown: true));
    }

    [Fact]
    public void CtrlTappedAndReleased_ThenS_IsPlainChar()
    {
        var f = new KeyFormatter();
        f.Format(VKCodes.CONTROL, State(VKCodes.CONTROL), isKeyDown: true);
        f.Format(VKCodes.CONTROL, State(), isKeyDown: false);
        var result = f.Format(0x53, State(), isKeyDown: true); // 'S' key
        Assert.Equal("s", result);
    }

    [Fact]
    public void InjectedMapper_IsUsed()
    {
        var f = new KeyFormatter(new StubMapper());
        var result = f.Format(0x41, State(), isKeyDown: true);
        Assert.Equal("@", result); // stub returns '@' for everything
    }

    private sealed class StubMapper : ICharMapper
    {
        public char? Map(int vk, byte[] keyboardState) => '@';
    }

    [Fact]
    public void Combo_WithMapper_UppercasesSingleCharTail()
    {
        var f = new KeyFormatter(new StubLowerMapper());
        f.Format(VKCodes.CONTROL, State(VKCodes.CONTROL), isKeyDown: true);
        var result = f.Format(0x53, State(VKCodes.CONTROL), isKeyDown: true); // 'S' key
        Assert.Equal("Ctrl + S", result);
    }

    private sealed class StubLowerMapper : ICharMapper
    {
        public char? Map(int vk, byte[] keyboardState) => 's';
    }

    [Theory]
    [InlineData(0xAD, "Mute")]
    [InlineData(0xAE, "Vol Down")]
    [InlineData(0xAF, "Vol Up")]
    [InlineData(0xB0, "Next Track")]
    [InlineData(0xB1, "Prev Track")]
    [InlineData(0xB2, "Stop")]
    [InlineData(0xB3, "Play/Pause")]
    [InlineData(0xA6, "Browser Back")]
    [InlineData(0xA7, "Browser Forward")]
    [InlineData(0xA8, "Browser Refresh")]
    [InlineData(0xA9, "Browser Stop")]
    [InlineData(0xAA, "Browser Search")]
    [InlineData(0xAB, "Browser Favorites")]
    [InlineData(0xAC, "Browser Home")]
    [InlineData(0xB4, "Mail")]
    [InlineData(0xB5, "Media")]
    [InlineData(0xB6, "App 1")]
    [InlineData(0xB7, "App 2")]
    [InlineData(0x5D, "Menu")]
    [InlineData(0x0C, "Clear")]
    [InlineData(0x29, "Select")]
    [InlineData(0x2A, "Print")]
    [InlineData(0x2B, "Execute")]
    [InlineData(0x2D, "Insert")]
    [InlineData(0x2F, "Help")]
    [InlineData(0x5F, "Sleep")]
    public void MediaBrowserLaunchKeys_ShowReadableNames(int vk, string expected)
    {
        var f = new KeyFormatter();
        Assert.Equal(expected, f.Format(vk, State(), isKeyDown: true));
    }

    [Theory]
    [InlineData(0x7C, "F13")]
    [InlineData(0x87, "F24")]
    [InlineData(0x15, "Kana")]
    [InlineData(0x16, "Junja")]
    [InlineData(0x17, "Final")]
    [InlineData(0x18, "Convert")]
    [InlineData(0x19, "Kanji")]
    [InlineData(0x1A, "NonConvert")]
    [InlineData(0x1C, "Mode Change")]
    public void MacroAndImeKeys_ShowReadableNames(int vk, string expected)
    {
        var f = new KeyFormatter();
        Assert.Equal(expected, f.Format(vk, State(), isKeyDown: true));
    }

    [Fact]
    public void F25_IsNotNamed()
    {
        var f = new KeyFormatter();
        Assert.Equal("<0x88>", f.Format(0x88, State(), isKeyDown: true));
    }

    [Theory]
    [InlineData(0xBA, "Semicolon")]
    [InlineData(0xBB, "Equals")]
    [InlineData(0xBC, "Comma")]
    [InlineData(0xBD, "Minus")]
    [InlineData(0xBE, "Period")]
    [InlineData(0xBF, "Slash")]
    [InlineData(0xC0, "Tilde")]
    [InlineData(0xDB, "Left Bracket")]
    [InlineData(0xDC, "Backslash")]
    [InlineData(0xDD, "Right Bracket")]
    [InlineData(0xDE, "Quote")]
    public void OemKeys_WithoutMapper_ShowReadableNames(int vk, string expected)
    {
        var f = new KeyFormatter();
        Assert.Equal(expected, f.Format(vk, State(), isKeyDown: true));
    }

    [Fact]
    public void OemKey_WithMapper_PrefersMappedCharacter()
    {
        var f = new KeyFormatter(new StubMapper());
        Assert.Equal("@", f.Format(0xBA, State(), isKeyDown: true));
    }
}