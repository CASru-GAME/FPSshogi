# Model integrity repair

The user requested inspection and repair of every game model, especially the bow and weapons. Preserve the supplied art's silhouette and materials wherever they are intact. Repair source conversion, disconnected parts, missing faces, alignment and scale at the generation source; do not replace the art style. Existing combat motion, game rules and UI remain the acceptance baseline.

## Task 1: Inventory and reproduce
- Record every imported model and every active weapon, piece, projectile and room prop. Render isolated models from multiple angles and compare source models against generated meshes.
- Record defects with images and measurable geometry relationships before changes. Include bow/string/arrow alignment, arrow flight orientation, blade surfaces, materials, room LODs and promoted pieces.

## Task 2: Repair confirmed defects
- Repair the reproducible asset builder and generated assets together. Prefer a transform or mesh repair to replacing original art.
- Add targeted geometry/assembly checks for confirmed defects. Run failing checks first, then verify the repaired asset and its actual rendered views.
- Keep all original FBX data available. Install a model editor only if Unity's mesh tools are insufficient.

## Task 3: Verify integration
- Inspect all repaired models in the real Title/Main flow, including two players, primary attacks, aiming, skills and promotions. Re-run motion, gameplay and art checks.
- Obtain one fresh read-only code review, address material findings, build the normal Windows player and run native smoke checks.
- Save a compact before/after gallery and Japanese findings report. Commit only this task's files; leave unrelated changes and prior evidence intact.

## Review focus
Mirrored transforms and triangle winding; weapon local axes and muzzle positions; bow limb/string/nock assembly throughout release; preservation of UVs and submesh materials; projectile appearance versus held appearance; near/far room LOD silhouettes.
