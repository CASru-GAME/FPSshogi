# Visual repair tools

The project already contains the restored/generated assets; these tools are only needed to reproduce them.

- `restore_room_textures.py <FPSshogi_Data>` requires Python and UnityPy. It exports 18 original textures and 22 material identities from the user's completed game. Asset file and path ID distinguish the two textures named 本. Renderer material slot assignments are recorded in `Assets/App/Art/RecoveredRoom/manifest.json`.
- `VisualRepairBuilder.BuildArtBatch` runs in the Unity editor or with `-batchmode -nographics -executeMethod VisualRepairBuilder.BuildArtBatch -quit`. It restores Field2 materials, generates environment LODs, bakes normalized weapon meshes, creates URP effect materials and assigns the Main scene references. Original FBX files remain intact.
- The mesh simplifier is the MIT-licensed Whinarn/UnityMeshSimplifier, copied under `Assets/Tests/Editor/ThirdParty`; it is excluded from player assemblies. See its `LICENSE.md`. Original repository: https://github.com/Whinarn/UnityMeshSimplifier.
- `VisualRepairChecks` runs the real Title/Main flow with two temporary virtual controllers when `.utmp/visual-fix/check.txt` contains an evidence directory name. Results are written under `VisualReview/<name>`. Remove the trigger to avoid an unwanted test launch. The runner removes its virtual devices on exit.
- `UiRegressionRunner` and `UiBuildRunner` remain the UI regression and Windows build entry points.

Mesh regeneration is best done with the editor closed in batch mode. An in-session replacement of several large meshes triggered `BufferD3D12::BeginWrite` in this machine's Unity 6.2 editor; the launcher uses Direct3D 11 for this environment. This is an editor rendering workaround, not a change to game rules.
