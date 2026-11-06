# FPSshogi visual repair

The user has approved fixing all eleven findings and proposed remedies in `VISUAL-REVIEW.txt`. This document records implementation choices within that authorization.

Preserve the original shogi pieces, Japanese room, game rules, local two-player UI, and damage/cooldown values. Restore the original room textures from the finished game. Correct both players' weapon presentation, attack/recovery motion, skill effects, and camera lifecycle. Make the scene lighter without visibly damaging its silhouette or UVs. Keep the Unity project editable and rebuild the Windows executable.

## Approach

Use original geometry and recovered textures. Author readable weapon mounts per type and an explicit idle/attack/recovery presentation; the current single-state imported controllers are not a reliable presentation pipeline. A procedural transform animation using the original static weapon geometry is acceptable and easier to make consistent than preserving broken rig poses. Preserve original source assets for editing. A completely new art direction or new gameplay is outside scope.

Use serialized prefabs/materials/native meshes for persistent art changes and a small runtime presentation layer for aiming, attacks, feedback and cleanup. Generate optimized environmental LOD meshes in the editor using the MIT UnityMeshSimplifier source, editor-only. Keep original FBX files; simplify collision only for heavy room props. Match materials by original renderer and material slot, with texture identity based on asset file/path ID rather than name.

## Acceptance

- V01: all original background texture assignments used by Title/Main are present, including distinct same-name book textures; wood/washi/tatami show in screenshots.
- V02/V06/V07: all eight weapon types have a readable idle pose, follow pitch/yaw, animate only on action, return to idle, and use a matching attack interval. World and split-screen views remain coherent; no old weapon survives a duel.
- V03: slow field is translucent purple, not opaque white, with visible ground and a readable boundary. Works in the built player.
- V04: three turrets spawn with sightline clearance and a visible gunbai gesture; all expire on time or duel exit.
- V05: zoom and pitch reset after every duel; pause preserves the current duel view and motion state.
- V08/V11: wood/leather/metal and red/blue weapons are distinct; guard/defense buffs, muzzle flash and impacts communicate action without covering the crosshair.
- V09: the four heavy props have substantially reduced triangle counts, separate simple collision, and LODs. No holes or UV corruption in inspected room views; report measured counts rather than promising a frame rate.
- V10: board positions are uniform and aligned with the visible grid, piece sizes leave margins, and promotion/drop/capture retain correct faces and ownership.
- Existing UI regression, actual play-mode visual regression, Windows build and native smoke check succeed.

## Implementation choices

Continue in the opened project on a new local feature branch: ignored restored FBX/images and the active Unity editor make a separate incomplete worktree unsuitable. Use warm wood, dark leather, steel, restrained red/blue lacquer and existing UI colors. Add no downloaded art assets. Verification uses virtual gamepads because two physical gamepads are unavailable.
