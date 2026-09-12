using System.Text;

namespace TypeReader.Core;

public class KeyFormatter
{
    private static readonly Dictionary<int, string> NamedKeys = new()
    {
        [0x0D] = "Enter", [0x1B] = "Esc", [0x20] = "Space", [0x08] = "Backspace",
        [0x09] = "Tab", [0x2E] = "Delete", [0x24] = "Home", [0x23] = "End",
        [0x21] = "Page Up", [0x22] = "Page Down", [0x25] = "←", [0x26] = "↑",
        [0x27] = "→", [0x28] = "↓", [0x2C] = "Print Screen", [0x14] = "Caps Lock",
        [0x90] = "Num Lock", [0x91] = "Scroll Lock", [0x13] = "Pause",
        [0x1C] = "Left Ctrl", [0x1D] = "Right Ctrl",
    };

    private static readonly Dictionary<int, string> ModifierNames = new()
    {
        [VKCodes.CONTROL] = "Ctrl", [VKCodes.SHIFT] = "Shift", [VKCodes.ALT] = "Alt",
        [VKCodes.LWIN] = "Win", [VKCodes.RWIN] = "Win",
    };

    private readonly List<int> _heldModifiers = new();

    public string? Format(int vkCode, bool[] keyboardState, bool isKeyDown)
    {
        if (VKCodes.IsModifier(vkCode))
        {
            if (!isKeyDown)
                return null; // modifier release never changes display
            _heldModifiers.Add(vkCode);
            return ModifierNames[vkCode];
        }

        if (!isKeyDown)
            return null;

        // Non-modifier pressed: flush combo if modifiers held
        if (_heldModifiers.Count > 0)
        {
            var combo = new StringBuilder();
            foreach (var mod in _heldModifiers.Distinct())
                combo.Append(ModifierNames[mod]).Append(" + ");
            var tail = NamedChar(vkCode, keyboardState);
            // Combo convention (design spec): letters render uppercase, e.g. Ctrl + S
            if (tail.Length == 1 && char.IsLower(tail[0]) && !keyboardState[VKCodes.SHIFT])
                tail = tail.ToUpperInvariant();
            combo.Append(tail);
            _heldModifiers.Clear();
            return combo.ToString();
        }

        return NamedChar(vkCode, keyboardState);
    }

    private static string NamedChar(int vkCode, bool[] keyboardState)
    {
        if (NamedKeys.TryGetValue(vkCode, out var name))
            return name;
        if (vkCode is >= 0x70 and <= 0x7B)
            return "F" + (vkCode - 0x6F); // F1..F12
        // Character keys: map via US-layout ASCII fallback (ToUnicodeEx is
        // wired in production by the hook layer; see Task 3 Step 5).
        return MapVkToChar(vkCode, keyboardState[VKCodes.SHIFT]);
    }

    internal static string MapVkToChar(int vkCode, bool shifted)
    {
        if (vkCode is >= 0x41 and <= 0x5A) // A-Z
            return shifted ? ((char)('A' + vkCode - 0x41)).ToString()
                           : ((char)('a' + vkCode - 0x41)).ToString();
        if (vkCode is >= 0x30 and <= 0x39) // 0-9
        {
            if (!shifted) return ((char)('0' + vkCode - 0x30)).ToString();
            return (vkCode - 0x30) switch
            {
                0 => ")", 1 => "!", 2 => "@", 3 => "#", 4 => "$",
                5 => "%", 6 => "^", 7 => "&", 8 => "*", _ => "(",
            };
        }
        return $"<0x{vkCode:X2}>";
    }
}