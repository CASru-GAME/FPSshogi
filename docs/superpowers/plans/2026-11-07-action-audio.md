# Action Audio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace placeholder ticks and silent actions with appropriate, licensed sound throughout FPSshogi.

**Architecture:** A Resources catalog and persistent pooled `App.Audio.GameAudio` service serve semantic `SoundCue` events. Gameplay hooks run only after an action succeeds, while UI focus/activation and session state supply interface events. Per-player movement components track real displacement and contacts.

**Tech Stack:** Unity 6000.2.12f1, C#, Python audio preparation, CC0 WAV assets.

**Spec:** docs/superpowers/specs/2026-11-07-action-audio.md

## Global Constraints

- Preserve combat rules, current controls, Japanese room and pieces.
- Free commercial-use assets; preserve source and license for every derived clip.
- Two-player sound remains balanced regardless of the active camera.
- Preserve unrelated dirty workspace files; stage only this task's files.

## Review Focus

- Rejected cooldown inputs must not play attack/skill sounds.
- Pause, disconnect and scene changes must not leave gameplay sound running.
- Mouse and controller UI activation must each play one semantic sound.
- Master/interface/effects mute must work immediately and persist.
- Repeated shots, movement and two simultaneous players must not clip or allocate unbounded voices.

### Task 1: Asset catalog and complete event integration

**Files:** `Tools/prepare_audio.py`, `Assets/App/Resources/Audio/`, `Assets/App/Scripts/Audio/`, existing Player actions/status/movement, ShogiBoard/ShogiPointer and UI scripts, `Assets/Tests/Editor/AudioRegression.cs`, `Assets/Tests/Runtime/AudioPlayerSmoke.cs`, `docs/audio-review-2026-11-07.md`.

**Interfaces:** `GameAudio.Play(SoundCue, Player owner=null, Vector3? position=null)`, `SetCombatPaused(bool)`, `StopCombat(Player owner=null)`, `GetVolume/SetVolume(AudioBus)`. All clips and gains come from a serialized Resources catalog.

- [x] Add a catalog/coverage check and record the current missing assets/service as RED.
- [x] Prepare and validate licensed source clips, manifest and an audition page.
- [x] Add bounded UI/gameplay source pools, gain/pan/variation, pause and volume persistence.
- [x] Connect accepted gameplay/board/UI events and actual movement; remove generated tones and HUD-triggered impact audio.
- [x] Extend meaningful runtime regressions for cooldowns, volume/mute, pause/resume, cleanup and UI duplication.
- [x] Run audio and existing gameplay/UI/selection/motion tests. Build Windows and capture native output.
- [x] Request a fresh read-only whole-change review, address findings and re-run relevant checks.
- [x] Commit only task-owned files and report build, evidence and material limits.
