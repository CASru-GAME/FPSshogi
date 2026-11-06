# FPSshogi Visual Repair Implementation Plan

> Execute with superpowers:executing-plans. Use one fresh final code review.

**Goal:** Resolve V01–V11 from the approved visual review and provide a verified Unity project and Windows build.
**Architecture:** Serialized restored/generated art assets plus a focused runtime weapon/effect layer, verified in actual Title/Main play mode.
**Tech Stack:** Unity 6000.2.12f1, C#, URP 17.2, Input System, Python/UnityPy for source restoration, editor-only UnityMeshSimplifier.
**Spec:** `docs/superpowers/specs/2026-11-06-visual-repair-design.md`

## Global constraints

Preserve original source models and user changes. Preserve game rules and UI. Do not push or publish. Use the current opened workspace and a local feature branch. Treat V08/V11 as approved visual improvements, not a new art direction.

## Review focus

- Repeated duels, zoom, pause and scene changes must clean up weapons, turrets and effects without leaking objects/materials.
- Both split-screen cameras must see the correct weapon and feedback; pitch extremes must not hide or explode geometry.
- Transparent materials and runtime effects must survive player shader stripping.
- Material slots, duplicate texture names and LOD UV seams must keep original art assignments.
- Captures, promotion and pawn drops must keep models aligned with HUD highlights and board cells.

## Tasks

### 1. Regression evidence and editor tooling
- [x] Preserve a baseline and write real play-mode checks for zoom carryover, action return-to-idle, skill visibility, scene cleanup and board spacing.
- [x] Run the checks against current code; retain expected failures and screenshots.
- [x] Add repeatable editor asset generation and source-identity texture export tooling.

### 2. Restore materials and optimize environment (V01/V08/V09)
- [x] Export original textures/material renderer slots to `Assets/App/Art` and apply to Field2.
- [x] Generate readable weapon materials using existing geometry.
- [x] Generate prop LODs and simple collision; record before/after triangle counts.
- [x] Inspect room screenshots and check assignments and bounds.

### 3. Weapon and camera presentation (V02/V05/V06/V07)
- [x] Generate per-weapon normalized prefabs with explicit mount settings.
- [x] Implement idle/action/recovery motion, view following, consistent attack timing and cleanup.
- [x] Reset zoom/pitch at duel boundaries; preserve paused state.
- [x] Run all eight weapon cases and repeated-duel regressions; inspect 1080p and 720p images.

### 4. Skill and combat feedback (V03/V04/V11)
- [x] Fix URP slow-field material and ground placement.
- [x] Separate gunbai and turret references; place and size turrets with clear sightline.
- [x] Add guard/buff feedback, muzzle flash/tracer/impact and cleanup.
- [x] Verify with actual input, pause, duel exit and built-player shader usage.

### 5. Board alignment (V10)
- [x] Derive equal cell centers from the visible grid; size pieces to fit while retaining type differences.
- [x] Verify initial arrangement, all six promoted types, captures and drops against HUD highlights.

### 6. Integration and delivery
- [x] Run visual and existing UI suites; inspect screenshots; fix relevant failures.
- [x] Obtain fresh code review, resolve important findings, and run affected regressions.
- [x] Build Windows, check native launch, document all eleven outcomes and measured geometry changes.
- [x] Commit only task changes and leave the Unity project ready to open/play.
