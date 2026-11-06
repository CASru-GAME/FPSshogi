# Player Selection Implementation Plan

> Execute inline with superpowers:executing-plans and a fresh final code review. The user already requested the concrete flow; proceed without an additional approval loop.

**Goal:** Device-owned participation and readiness before every new local match.
**Architecture:** LocalMatchDevices owns assignments/readiness; PlayerSelectionPanel renders cards; GameSessionUI handles per-device button edges and guarded scene transition.
**Tech Stack:** Unity6000.2.12f1, UGUI, Input System, Windows.
**Spec:** docs/superpowers/specs/2026-11-06-player-selection-design.md

## Constraints and review focus
Preserve gameplay/art and unrelated restoration files. Work in the current restored project on a new local branch. Keep two-pad and either mixed joining order, refuse duplicate/unjoined preparation, preserve surviving slots during disconnect, prevent held-event leakage, and require renewed preparation on rematch.

## 1. Selection behavior
- [x] Add Assets/Tests/Editor/PlayerSelectionRegression.cs. Trigger .utmp/player-selection/run.txt, save SelectionReview/<run>/ results/errors/screenshots. Prove current StartMatch bypasses selection before production edits.
- [x] Extend Assets/App/Scripts/UI/LocalMatchDevices.cs with BeginSelection(bool), PlayerFor(InputDevice), Confirm(InputDevice), Cancel(InputDevice), IsPrepared(int), AllPrepared; keep Ready as both-connected for gameplay pause. Refresh never autojoins empty slots; reconnect replacement only for previously assigned active-match slots.
- [x] Add Assets/App/Scripts/UI/PlayerSelectionPanel.cs and integrate MenuPanels.Selection. Two cards, device descriptions, per-player prompts, keyboard/mouse action and title button; update without rebuilding every frame.
- [x] Integrate GameSessionUI.Selection page, per-device edges/release gate, one-second ready delay, cancellation and rematch reset. StartMatch only opens selection; private transition loads Main only when both prepared and connected.
- [x] Run real input fixtures for two pads, both mixed orders, duplicate/third devices, held input, cancellation, disconnect and rematch. Fix failures before proceeding.

## 2. Integration and verification
- [x] Add conditional SelectionTestInput helper for existing editor/native harnesses to join and prepare via Input System; update UiRegressionRunner, GameplayRegressionRunner, VisualRepairChecks and native smoke entry.
- [x] Run selection suite at1080p and720p; inspect screenshots/text fits. Run existing UI, gameplay and visual suites after adapting their startup expectations.
- [x] Extend conditional Windows smoke coverage for actual selected pairings and mixed mode; keep helpers absent from shipping assembly.
- [x] Fresh read-only whole-diff code review, resolve important findings and retest affected behavior.

## 3. Delivery
- [x] Rebuild/run normal Windows player, verify title/selection/join/cancel UI and excluded test helpers.
- [x] Save PLAYER-SELECTION.txt with controls, evidence and verification limits. Selectively commit on the local branch and leave Unity ready to run.
