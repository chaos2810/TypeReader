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
}