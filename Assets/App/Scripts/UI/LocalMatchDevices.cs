using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace App.UI
{
    // Selection is explicit. Reconnection may only replace an already assigned slot.
    public sealed class LocalMatchDevices
    {
        readonly InputDevice[][] slots = new InputDevice[2][];
        readonly string[] schemes = new string[2];
        readonly bool[] prepared = new bool[2];
        readonly bool[] wasConnected = new bool[2];
        public bool Ready => Connected(0) && Connected(1);
        public bool AllPrepared => Ready && prepared[0] && prepared[1];
        public bool HasParticipants => slots.Any(slot => slot != null);
        public bool Connected(int slot) => slots[slot] != null && slots[slot].Length > 0 && slots[slot].All(d => d != null && d.added && d.enabled);
        public bool IsGamepad(int slot) => schemes[slot] == "Gamepad";
        public InputDevice[] DevicesFor(int slot) => slots[slot]?.ToArray() ?? System.Array.Empty<InputDevice>();
        public bool IsPrepared(int slot) => Connected(slot) && prepared[slot];
        public string Description(int slot) => !Connected(slot) ? (slots[slot] == null ? "参加待ち" : "接続が切れました") : IsGamepad(slot) ? "コントローラー\n" + slots[slot][0].displayName : "キーボード + マウス";
        public int PlayerFor(InputDevice device)
        {
            if (device == null) return -1;
            for (int i = 0; i < slots.Length; i++) if (slots[i] != null && slots[i].Contains(device)) return i;
            return -1;
        }
        public void BeginSelection(bool keepAssignments)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                prepared[i] = false;
                if (!keepAssignments) { slots[i] = null; schemes[i] = null; }
                wasConnected[i] = Connected(i);
            }
        }
        public bool Confirm(InputDevice device)
        {
            if (device == null || !device.added || !device.enabled || !(device is Gamepad || device is Keyboard)) return false;
            int slot = PlayerFor(device);
            if (slot >= 0 && Connected(slot))
            { bool changed = !prepared[slot]; prepared[slot] = true; return changed; }
            if (slot < 0) slot = System.Array.FindIndex(slots, entry => entry == null || entry.Any(d => d == null || !d.added || !d.enabled));
            if (slot < 0) return false;
            InputDevice[] selection;
            if (device is Gamepad) selection = new[] { device };
            else
            {
                if (Mouse.current == null || !Mouse.current.added || !Mouse.current.enabled) return false;
                selection = new InputDevice[] { device, Mouse.current };
            }
            if (selection.Any(d => PlayerFor(d) >= 0 && PlayerFor(d) != slot && Connected(PlayerFor(d)))) return false;
            slots[slot] = selection; schemes[slot] = device is Gamepad ? "Gamepad" : "Keyboard&Mouse";
            prepared[slot] = false; wasConnected[slot] = true; return true;
        }
        public bool Cancel(InputDevice device)
        {
            int slot = PlayerFor(device); if (slot < 0) return false;
            if (prepared[slot]) prepared[slot] = false;
            else { slots[slot] = null; schemes[slot] = null; wasConnected[slot] = false; }
            return true;
        }
        public bool Refresh(bool reconnectMissing = false,System.Action<int> disconnected=null,System.Action<int> reconnected=null)
        {
            bool changed = false;
            var used = new HashSet<InputDevice>();
            for (int i=0; i<2; i++) if (Connected(i)) foreach (var d in slots[i]) used.Add(d);
            for (int i=0; i<2; i++)
            {
                bool connected = Connected(i);
                if (connected != wasConnected[i]) {
                    changed = true; wasConnected[i] = connected;
                    if(connected)reconnected?.Invoke(i);else disconnected?.Invoke(i);
                }
                if (connected) continue;
                prepared[i] = false;
                if (!reconnectMissing || slots[i] == null) continue;
                InputDevice[] replacement = null; string scheme = null;
                if (schemes[i] == "Gamepad")
                {
                    var pad = Gamepad.all.FirstOrDefault(p => p.added && p.enabled && !used.Contains(p));
                    if (pad != null) { replacement = new InputDevice[] { pad }; scheme = "Gamepad"; }
                }
                else if (Keyboard.current != null && Keyboard.current.enabled && Mouse.current != null && Mouse.current.enabled && !used.Contains(Keyboard.current) && !used.Contains(Mouse.current))
                { replacement = new InputDevice[]{Keyboard.current, Mouse.current}; scheme = "Keyboard&Mouse"; }
                if (replacement == null) continue;
                slots[i] = replacement; schemes[i] = scheme; changed = true; wasConnected[i] = true;
                reconnected?.Invoke(i);
                foreach (var d in replacement) used.Add(d);
            }
            return changed;
        }
        public void Pair(PlayerInput player, int slot)
        {
            if (!Connected(slot) || player == null) return;
            player.neverAutoSwitchControlSchemes = true;
            if (player.devices.Count == slots[slot].Length && player.devices.All(d => slots[slot].Contains(d))) return;
            player.SwitchCurrentControlScheme(schemes[slot], slots[slot]);
        }
    }
}
