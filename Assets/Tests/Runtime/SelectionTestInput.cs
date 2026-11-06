#if UNITY_EDITOR || FPS_GAMEPLAY_SMOKE || FPS_VISUAL_SMOKE || FPS_SELECTION_SMOKE
using System.Collections;
using App.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Test-only input choreography; all assignments/readiness still go through the UI.
public static class SelectionTestInput
{
    public static IEnumerator Tap(InputDevice device, bool cancel = false)
    {
        if (device is Gamepad pad) InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(cancel ? GamepadButton.East : GamepadButton.South));
        else if (device is Keyboard keyboard) InputSystem.QueueStateEvent(keyboard, new KeyboardState(cancel ? Key.Escape : Key.Enter));
        yield return .15d;
        if (device is Gamepad releasedPad) InputSystem.QueueStateEvent(releasedPad, new GamepadState());
        else if (device is Keyboard releasedKeyboard) InputSystem.QueueStateEvent(releasedKeyboard, new KeyboardState());
        yield return .18d;
    }
    public static IEnumerator TwoGamepads(Gamepad first, Gamepad second, bool open = true)
    {
        if (open) GameSessionUI.Instance.StartMatch();
        yield return .35d;
        foreach (var pad in new[] { first, second })
        {
            int slot = GameSessionUI.Instance.Devices.PlayerFor(pad);
            if (slot >= 0 && GameSessionUI.Instance.Devices.Connected(slot)) continue;
            var tap = Tap(pad); while (tap.MoveNext()) yield return tap.Current;
        }
        foreach (var pad in new[] { first, second })
        {
            int slot = GameSessionUI.Instance.Devices.PlayerFor(pad);
            if (slot >= 0 && GameSessionUI.Instance.Devices.IsPrepared(slot)) continue;
            var tap = Tap(pad); while (tap.MoveNext()) yield return tap.Current;
        }
        yield return 1.5d;
    }
}
#endif
