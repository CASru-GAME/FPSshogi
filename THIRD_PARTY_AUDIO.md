# FPSshogi audio credits

All source recordings used in the shipped effects are **CC0 1.0 Universal** (public-domain dedication). Commercial use, modification and redistribution are permitted. No paid asset or noncommercial-only recording is included.

| Creator | Collection / recording | Used for |
| --- | --- | --- |
| Kenney | [Impact Sounds](https://kenney.nl/assets/impact-sounds) | Wood contacts, footsteps, cloth/body impacts, metal/bell layers |
| Kenney | [RPG Audio](https://kenney.nl/assets/rpg-audio) | Cloth, leather, weapon handling, blade and page sounds |
| artisticdude | [Swishes Sound Pack](https://opengameart.org/content/swishes-sound-pack) | Spear, sword, naginata, knife and movement whooshes |
| kurt | [Gunshots — Black Powder.wav](https://opengameart.org/content/gunshots) | Matchlock guns, turrets, flashbang transient |
| Hanbaal | [bow2.wav](https://freesound.org/people/Hanbaal/sounds/178873/) | Bow release; prepared from the publicly downloadable HQ MP3 preview |

License: <https://creativecommons.org/publicdomain/zero/1.0/>
Legal text: <https://creativecommons.org/publicdomain/zero/1.0/legalcode>

Selected originals are retained in `AudioSources/raw/`. `AudioSources/manifest.json` records each derived WAV's original path, source URL, author, CC0 license, original and derived SHA-256 hashes, layers, pitch/resampling, trims and filtering. Kenney's supplied license notices are retained under `AudioSources/licenses/`.

Processing by the FPSshogi project: trimming, mono conversion, fades, filtering, restrained pitch changes and layering; peak headroom preserved. Rebuild with `Tools/prepare_audio.py`, validate with `Tools/check_audio_assets.py`. These source/derived sounds remain reusable under CC0; this notice does not change the project's code license.
