using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace App.UI
{
    // Keep the surviving player's device when a controller disconnects.
    public sealed class LocalMatchDevices
    {
        readonly InputDevice[][] slots = new InputDevice[2][];
        readonly string[] schemes = new string[2];
        public bool Ready => Connected(0) && Connected(1);
        public bool Connected(int slot) => slots[slot] != null && slots[slot].Length > 0 && slots[slot].All(d => d != null && d.added && d.enabled);
        public bool IsGamepad(int slot) => schemes[slot] == "Gamepad";
        public string Description(int slot) => !Connected(slot) ? "未接続" : IsGamepad(slot) ? "ゲームパッド" : "キーボード + マウス";
        public bool Refresh(bool preferGamepads = false)
        {
            bool changed = false;
            // Before the match, plugging in two pads should choose the two-pad setup.
            var pads = Gamepad.all.Where(p=>p.added && p.enabled).Take(2).ToArray();
            if(preferGamepads && pads.Length==2)
            {
                for(int i=0;i<2;i++)
                    if(slots[i]==null || slots[i].Length!=1 || slots[i][0]!=pads[i])
                    { slots[i]=new InputDevice[]{pads[i]}; schemes[i]="Gamepad"; changed=true; }
                return changed;
            }
            var used = new HashSet<InputDevice>();
            for (int i=0; i<2; i++) if (Connected(i)) foreach (var d in slots[i]) used.Add(d);
            for (int i=0; i<2; i++)
            {
                if (Connected(i)) continue;
                var pad = Gamepad.all.FirstOrDefault(p => p.added && p.enabled && !used.Contains(p));
                InputDevice[] replacement = null; string scheme = null;
                // A missing pad waits for a pad; do not silently give that player another player's keyboard.
                if (pad != null) { replacement = new InputDevice[]{pad}; scheme = "Gamepad"; }
                else if (schemes[i] != "Gamepad" && Keyboard.current != null && Mouse.current != null && !used.Contains(Keyboard.current))
                { replacement = new InputDevice[]{Keyboard.current, Mouse.current}; scheme = "Keyboard&Mouse"; }
                if (replacement == null) continue;
                slots[i] = replacement; schemes[i] = scheme; changed = true;
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
