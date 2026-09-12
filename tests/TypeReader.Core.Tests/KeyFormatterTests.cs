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
}