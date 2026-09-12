using System.Runtime.InteropServices;
using System.Text;

namespace TypeReader.Core;

public sealed class ToUnicodeCharMapper : ICharMapper
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(uint vk, uint scanCode, byte[] lpKeyState,
        [Out] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr hkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    public char? Map(int vk, byte[] keyboardState)
    {
        var buffer = new StringBuilder(8);
        int result = ToUnicodeEx((uint)vk, 0, keyboardState, buffer, buffer.Capacity, 0,
            GetKeyboardLayout(0));
        if (result == 1 && buffer.Length > 0)
        {
            var c = buffer[0];
            if (char.IsControl(c)) return null;
            return c;
        }
        return null;
    }
}
