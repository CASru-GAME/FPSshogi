# Visual repair tools

The project already contains the restored/generated assets; these tools are only needed to reproduce them.

- `restore_room_textures.py <FPSshogi_Data>` requires Python and UnityPy. It exports 18 original textures and 22 material identities from the user's completed game. Asset file and path ID distinguish the two textures named 本. Renderer material slot assignments are recorded in `Assets/App/Art/RecoveredRoom/manifest.json`.
- `VisualRepairBuilder.BuildArtBatch` runs in the Unity editor or with `-batchmode -nographics -executeMethod VisualRepairBuilder.BuildArtBatch -quit`. It restores Field2 materials, generates environment LODs, bakes normalized weapon meshes, creates URP effect materials and assigns the Main scene references. Original FBX files remain intact.
- The mesh simplifier is the MIT-licensed Whinarn/UnityMeshSimplifier, copied under `Assets/Tests/Editor/ThirdParty`; it is excluded from player assemblies. See its `LICENSE.md`. Original repository: https://github.com/Whinarn/UnityMeshSimplifier.
- `VisualRepairChecks` runs the real Title/Main flow with two temporary virtual controllers when `.utmp/visual-fix/check.txt` contains an evidence directory name. Results are written under `VisualReview/<name>`. Remove the trigger to avoid an unwanted test launch. The runner removes its virtual devices on exit.
- `UiRegressionRunner` and `UiBuildRunner` remain the UI regression and Windows build entry points.
- `python Tools/validate_visual_meshes.py` checks the 23 generated mesh index buffers and writes `VisualReview/mesh-validation.txt`. Regeneration updates existing meshes through Unity's mesh API, which refreshes vertex/index rendering buffers; generic serialized-object copying left stale buffers in this editor session.
- `python Tools/create_visual_gallery.py` regenerates `VisualReview/fixed.html` from the retained final screenshots and verification reports.
- `FPSshogi > Build visual verification player` builds `Builds/VisualSmoke` with the temporary `FPS_VISUAL_SMOKE` compilation flag. Its runner checks native URP materials and captures the slow field, gun flash, silver buff and king fan at runtime; output is under `VisualReview/PlayerSmoke`. The runner is excluded from the normal Windows build.

Mesh regeneration is best done with the editor closed in batch mode. An in-session replacement of several large meshes triggered `BufferD3D12::BeginWrite` in this machine's Unity 6.2 editor; the launcher uses Direct3D 11 for this environment. This is an editor rendering workaround, not a change to game rules.

## Model integrity audit (2026-11-07)

- `ModelIntegrityAudit` watches `.utmp/model-repair/audit.txt` and writes six-direction model views plus imported/active mesh inventories under `VisualReview/Models-<name>`. It uses isolated preview scenes and does not modify game scenes. `Compare` captures unreduced source weapon geometry under the display materials.
- `ModelIntegrityChecks` watches `.utmp/model-repair/check.txt`. It checks bow assembly, held/fired arrow consistency, barrel/muzzle alignment, katana seam preservation, collapsed weapon faces, and all prefab mesh/material assignments.
- The `weapons` builder command removes the imported bow's fixed string, aligns the limb plane, builds string/nock anchors, preserves the katana blade, cleans collapsed triangles and produces shared arrow projectile prefabs. Original FBX files are retained.
- `Tools/create_model_gallery.py` uses Pillow to generate the compact view-switching gallery from `Models-Baseline`, `Models-Final` and `Motion-Models-Verified` captures.
- A visual-check name containing `Motion` and `Flight` runs the isolated gravity/arrow-pose regression. A name containing `Motion` otherwise runs all character motions, including string attachment and arrow handoff checks.
- `.utmp/visual-fix/build-art.txt` with `visual-player` builds the separate `Builds/VisualSmoke` executable. It checks all eight weapon types and takes bow release/redraw captures. Pass `-visualReviewOutput <directory>` to preserve earlier native evidence. The normal Windows game excludes this test runner.
