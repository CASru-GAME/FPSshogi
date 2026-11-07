# FPSshogi action audio

The user requests appropriate free sounds for every action, replacing the cheap existing electronic ticks. Keep the current Japanese room, pieces, combat rules, controls and visual design.

Use CC0 recordings from Kenney (Impact Sounds and RPG Audio), artisticdude (Swishes), Hanbaal (bow2) and kurt (black powder gunshot). Preserve author, source, license and processing provenance. Edit short mono clips with fades, consistent headroom and several variations for repeated actions. Wood suits board/UI interaction; cloth, air and metal suit the melee weapons; a real bow and black powder gun distinguish ranged weapons. Skills get distinct, restrained cues rather than a common beep. Do not add continuous music to cover missing feedback.

The audio service has separate UI and gameplay pools. It survives scene changes, uses no synthetic tone fallback, supports master/effects/interface volume, and pauses gameplay voices with gameplay. Two local players share a mix with modest left/right panning, independent of the single scene listener. Spatial events use distance to both players. No repeated source allocation, overlapping navigation spam or stale effects after a duel.

Play at accepted actions, not raw input or HUD polling: all eight primary and skill actions, guarding/aiming, arrow re-nocking, projectile impact, grenade detonation, turret fire, actual damage/block, grounded footsteps/climb/landing, board cursor/select/cancel/move/drop/promotion, menu navigation/confirm/back/invalid, device join/ready/disconnect, countdown/start and results. Cooldown rejection stays silent; errors in board/UI receive quiet feedback.

Verify clip integrity, event coverage, accepted/rejected action behavior, both-player mixing, mute, volume persistence, pause/resume and cleanup. Run existing gameplay/UI/motion regressions, compile and build Windows, and capture native mixed output to check nonzero output, headroom and silence when muted. Provide an audition page for individual sounds and clear attribution.
