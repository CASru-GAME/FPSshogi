# FPSshogi UI repair

The user authorized fixing all findings in `UI-REVIEW.txt`. Keep the restored Unity project runnable in this workspace and retain its existing Japanese/wood visual identity.

## Scope
- U01: per-camera player label, HP, crosshair, damage flash, weapon/skill cooldowns and control hints.
- U02/U03/U09: correct captured-piece counts/removal, upright readable labels and contained arrangement.
- U04/U05: turn label, cursor/selection/legal-target distinction, visible captured mode, invalid-action feedback.
- U06/U07/U08: scalable title, keyboard/gamepad focus, help/rules, local device readiness and disconnect handling.
- U10/U11/U12: pause/help/sensitivity, resume/return, short duel countdown/result, persistent match result and rematch.
- Repair the discovered non-pawn nifu check because it blocks the captured-piece workflow.

## Approach
Use the existing uGUI and Input System packages. Add focused runtime UI components built from a shared theme, with a bundled licensed Japanese font. Keep the existing 3D scenery and game rules, with small observable APIs on the board/pointer/player for the UI. A new scene-authored prefab hierarchy would entail fragile manual scene rewiring; an IMGUI overlay would provide worse scaling/navigation. Runtime uGUI is the implementation choice, not a claimed user preference.

Reference canvas is 1920x1080, scaled to fit; verify 1280x720 and 1920x1080. Palette: dark brown panels, parchment text, gold selection, cyan legal targets. Each player has separate HUD inside its own camera viewport. Board input and menu navigation must not act simultaneously.

This remains a local two-player game: two gamepads, or keyboard/mouse plus one gamepad. Explain this before start. Missing devices block/resume the current match without losing state; do not invent an AI opponent.

Pause is a UI overlay with time/input suppression, preserving the underlying match state, camera, HP and weapon objects. Do not enter the existing Paused game state because its exit hooks destroy duel objects. Countdown uses unscaled time. No automatic five-second exit from match results.

## Validation
Meaningful Unity runtime regression checks for consumed P2 pieces, non-pawn drops, title focus/fit, selection/legal feedback, menu input suppression, cooldown/HP values, disconnect/reconnect, persistent result/rematch and scene cleanup. Visual screenshots of title/help, board, captured pieces, FPS, pause/result at the target resolutions. Physical controllers are unavailable; use virtual Input System devices and disclose that limitation.
