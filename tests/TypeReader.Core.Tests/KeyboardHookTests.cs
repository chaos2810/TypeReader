using TypeReader.Core;

namespace TypeReader.Core.Tests;

public class KeyboardHookTests
{
    [Fact]
    public void Install_OnDesktopSession_Succeeds()
    {
        using var hook = new KeyboardHook();
        hook.Install(); // throws InvalidOperationException if SetWindowsHookEx fails
        hook.Dispose(); // clean up immediately; no keys typed during test
    }

    [Fact]
    public void NormalizeModifier_MapsExtendedVKsToMaster()
    {
        Assert.Equal(0x10, KeyboardHook.NormalizeModifier(0xA0)); // LShift
        Assert.Equal(0x10, KeyboardHook.NormalizeModifier(0xA1)); // RShift
        Assert.Equal(0x11, KeyboardHook.NormalizeModifier(0xA2)); // LCtrl
        Assert.Equal(0x11, KeyboardHook.NormalizeModifier(0xA3)); // RCtrl
        Assert.Equal(0x12, KeyboardHook.NormalizeModifier(0xA4)); // LAlt
        Assert.Equal(0x12, KeyboardHook.NormalizeModifier(0xA5)); // RAlt
        Assert.Equal(0x41, KeyboardHook.NormalizeModifier(0x41)); // non-modifier unchanged
    }
}