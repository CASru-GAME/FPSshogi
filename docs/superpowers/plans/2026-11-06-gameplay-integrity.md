# FPSshogi Gameplay Integrity Implementation Plan

> Execute in the current branch with focused parallel repairs and one fresh final review. User already authorized audit and fixes.

**Goal:** Correct reproducible shogi/FPS gameplay faults and deliver a verified Unity project and Windows build.
**Architecture:** Existing board/state/player components, focused shared query/effect helpers, real Play Mode regressions.
**Tech stack:** Unity 6000.2.12f1, C#, Input System, URP.
**Spec:** `docs/superpowers/specs/2026-11-06-gameplay-integrity-design.md`

## Global constraints
Preserve user-confirmed FPS king-capture victory and automatic promotion. Preserve balance values, UI and art. No checkmate rules, push or publication. Preserve unrelated restoration edits. Use the active editor and current project with a new local branch.

## Review focus
Invalid actions during a duel must not alter saved attacker or board; multi-collider/overlapping effects must apply once and clean up independently; paused/countdown combat must not resolve or advance; both attacker colors and defender victories must update the correct hand and next turn; input and collisions must behave with actual scene scale and triggers.

### 1. Board and outcome consistency
- [x] Add `GameplayBoardChecks` and `GameplayRegressionRunner`; verify initial 40-piece setup, independently specified movement tables, blockers, promotion, pawn/drop constraints, attacker/defender wins, king victory and illegal mutation cases.
- [x] Run baseline to retain failures; fix `ShogiBoard` turn/drop validation and nifu; add selected simultaneous-death behavior in `DuelManager`, `GameStateHolder`, board and transition UI only as required.
- [x] Run board and actual duel-result regressions, including damage sources at different Update orders.

### 2. Movement and combat lifecycle
- [x] Add actual Player/input/physics checks for climb, mouse delta, pause and duel reset; reproduce failures.
- [x] Fix `Player` climb and input handling; integrate `EffectList.ClearEffects()` in duel/status reset; verify scene/pause cleanup.

### 3. Effect ownership and configured stats
- [x] `GameplayEffectsChecks.Run(Action<string,bool>)` must fail before edits.
- [x] Fix Slow/Flash/field reapplication and independent cleanup, multi-field/multi-collider occupancy, rejected guard rebind, and configured defense.
- [x] Parent runs real Play Mode checks before accepting changes, including trigger-only and disabled/destroyed field contacts.

### 4. Shot and projectile correctness
- [x] `GameplayProjectileChecks.Run(Action<string,bool>)` must fail before edits.
- [x] Fix single-hit arrows, launch-based range, swept collisions where needed, and trigger/owner filtering for shots while retaining solid cover. Extend to melee trigger/cover and paused turret checks after reproduced failures.
- [x] Parent runs physics fixtures and actual weapon checks. Actual Rigidbody arrow thin-cover control passes without additional collision-mode changes.

### 5. Integration and delivery
- [x] Run new gameplay, existing UI and visual regression suites; inspect relevant screenshots and errors. Gameplay199, UI52 at1080p and52 at720p, visual132, native19 all pass; runtime error logs empty.
- [x] Obtain independent whole-diff review; resolve important findings and retest affected behavior. Final reviewer accepts code after `AfterReview1080`: 199 PASS, no failures/errors.
- [x] Build and launch normal Windows version; retain concrete findings, fixes, test results and limitations in `GAMEPLAY-FIXES.txt` and `GameplayReview/`. Normal build has0 errors; conditional test classes absent; title/help/back/exit verified.
- [x] Commit task changes locally and leave Unity ready to open/play. Preserve the local branch and restored asset workspace; no remote push or merge is requested.
