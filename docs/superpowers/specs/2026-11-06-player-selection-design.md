# Player device selection

The user explicitly requested the proposed flow: participate using the intended device, confirm 1P/2P, and start only after both players are ready. Support two controllers and keyboard/mouse plus one controller. The prior proposal and current instruction authorize this feature; routine button/layout decisions below are implementation choices.

Keep Unity UGUI, existing Japanese theme and Input System. Title's start button opens a two-card selection screen; it no longer launches a match directly. Joining order selects 1P (first move, left FPS view) and 2P (second move, right FPS view). Show the assigned device and ready state on each card.

Controller A / keyboard Enter first joins and a separate press prepares that same player. B / Esc first cancels readiness, then leaves that slot; remaining players keep their slots. A mouse button may join/prepare/cancel the keyboard/mouse slot only. No device can occupy both slots, and no unjoined controller can ready another player. Extra connected controllers never override explicit choices. When both are prepared and connected, start after one second; cancellation/disconnection during that interval aborts it. Opening/held inputs cannot both join and prepare or leak into gameplay.

Keyboard/mouse is one logical participant and requires both devices. Both joining orders must work even when additional controllers are connected. The screen explains participation, preparation and cancellation. When no player is joined, B/Esc can go back; the visible title button always offers a mouse route back.

On rematch, retain device assignments and clear readiness, then use the same selection flow. Returning to title clears the selection. A disconnected slot loses its ready status and can be reclaimed; other slots remain stable. During an existing game preserve the current pause/reconnect behavior and surviving player's assignment. Keep all shogi, combat, balance and art unchanged.

Approach: extend LocalMatchDevices with explicit join/prepare/cancel state, add a focused PlayerSelectionPanel, integrate it through GameSessionUI/MenuPanels. Automatic connection-order assignment does not satisfy the requested participation step. A device-dropdown picker adds unnecessary indirection for two people sharing a screen.

Validate actual Input System button events and PlayerInput pairing in Play Mode and a Windows verification build, including two-pad and both mixed joining orders, duplicate/extra devices, held input, cancel, disconnect, rematch and layout at 1080p/720p. Existing test harnesses must enter the new flow using inputs, never bypass readiness in production. Preserve unrelated restoration edits and the local Unity workspace with its ignored source assets. No remote publication.
