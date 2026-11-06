# FPSshogi gameplay integrity

User request: audit and fix shogi rules and FPS behavior. User explicitly confirmed existing FPS-shogi rules: capturing the enemy king in a duel wins; promotion is automatic; ordinary shogi check/checkmate restrictions are not added.

Keep the restored art, completed UI, numeric weapon balance, and two-player local controls. Correct reproducible gameplay faults rather than redesigning the game. Use actual Unity Play Mode and physics fixtures, with failing regression evidence before changes. Parent controls the single Unity editor. Independent effect/projectile repairs have separate file ownership.

Known investigation targets: promoted pawn counted as nifu; board mutations outside the active turn; same-frame double deaths biased toward 2P; climb input never consumed; mouse delta scaled by frame duration; status defaults ignored; reapplied slow/flash stacking; field overlap cleanup; arrows damaging twice or measuring range from world origin; skill triggers blocking bullets. Validate movement tables, promotion, drops, captures, turn handoff, all weapon actions and effects, pause, scene cleanup, and result flow.

For simultaneous deaths, preserve both pieces and end the attacker's turn. The optional question offered this recommended default or replaying the duel; after ample time without a response, the parent explicitly stated and adopted the default. Resolve all damage in a frame before adjudicating the outcome, and describe the rule in the help screen.

Approach: make focused fixes in the current source components with reusable collision/effect helpers only where necessary. Retain the current project and branch because restored ignored source assets and the live editor are required. Do not reset restoration edits, push, or publish. A fresh code reviewer checks the final diff; parent verifies actual Unity and Windows player behavior.

Rule references: https://www.shogi.or.jp/knowledge/shogi/03.php and https://www.shogi.or.jp/knowledge/shogi/05.php. These support piece movement, promoted movement and nifu; the explicit FPS variant governs win/check/promotion choices.
