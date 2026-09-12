using System.Runtime.InteropServices;

namespace TypeReader.Core;

public delegate void KeyPressedEventHandler(int vkCode, bool[] keyboardState, bool isKeyDown);

public sealed class KeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly bool[] _keyboardState = new bool[256];

    public event KeyPressedEventHandler? KeyPressed;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc,
            NativeMethods.GetModuleHandle(module.ModuleName!), 0);
        if (_hookId == IntPtr.Zero)
            throw new InvalidOperationException(
                $"SetWindowsHookEx failed: {Marshal.GetLastWin32Error()}");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            bool isKeyDown = msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
            if (isKeyDown || msg is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP)
            {
                int vk = Marshal.ReadInt32(lParam); // first DWORD of KBDLLHOOKSTRUCT
                NativeMethods.GetAsyncKeyboardState(_keyboardState);
                KeyPressed?.Invoke(vk, (bool[])_keyboardState.Clone(), isKeyDown);
            }
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    ~KeyboardHook()
    {
        if (_hookId != IntPtr.Zero)
            NativeMethods.UnhookWindowsHookEx(_hookId);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}