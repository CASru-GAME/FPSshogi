"""Validate the shipped catalog, provenance, WAV encoding and mix headroom."""
import hashlib
import json
from pathlib import Path
import struct
import sys
import wave

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/App/Resources/Audio'

def check():
    catalog = ASSETS / 'catalog.json'
    assert catalog.is_file(), 'Missing authored audio catalog (only generated UI tones exist)'
    cues = json.loads(catalog.read_text(encoding='utf-8'))['cues']
    provenance = json.loads((ROOT / 'AudioSources/manifest.json').read_text(encoding='utf-8'))
    required = {'SpearSwing','SwordSwing','NaginataSwing','KnifeSwing','ShortGun','LongGun','BowRelease',
                'Leap','Throw','FlashBurst','Dash','DashStrike','GinShield','KinShield','Ascend','FieldCast',
                'Command','TurretShot','UiNavigate','UiConfirm','UiBack','UiInvalid','PlayerJoin','PlayerReady',
                'Disconnect','BoardMove','BoardDrop','Promote','Countdown','DuelStart','MatchWin','Hit','Block',
                'FootstepSoft','FootstepWood','Land','Climb','BowNock','AimIn','AimOut','GuardRaise','BuffEnd'}
    ids = [c['id'] for c in cues]
    assert len(ids) == len(set(ids)), 'Duplicate cue ID'
    assert required <= set(ids), f'Missing action cues: {required-set(ids)}'
    checked = 0; original_hashes={}
    for cue in cues:
        assert 0 < cue['gain'] <= 1 and 0 <= cue['pitch'] <= .08
        assert cue['clips'], cue['id']
        for clip in cue['clips']:
            path = ROOT / ('Assets/App/Resources/' + clip + '.wav')
            record = provenance['clips'][path.stem]
            assert record['sources'] and all(s['license'] == 'CC0-1.0' for s in record['sources'])
            for s in record['sources']:
                source=(ROOT/s['file']).resolve()
                assert source.is_relative_to((ROOT/'AudioSources/raw').resolve()), source
                if source not in original_hashes: original_hashes[source]=hashlib.sha256(source.read_bytes()).hexdigest()
                assert original_hashes[source]==s['sha256'], source
            assert record['sha256'] == hashlib.sha256(path.read_bytes()).hexdigest(), path
            with wave.open(str(path), 'rb') as w:
                assert (w.getnchannels(),w.getsampwidth(),w.getframerate()) == (1,2,44100), path
                samples=struct.unpack('<'+'h'*w.getnframes(), w.readframes(w.getnframes()))
            assert .025 < len(samples)/44100 < 4, path
            peak=max(abs(x) for x in samples)/32768
            assert .05 < peak <= .71, f'Clipped/quiet {path}: {peak}'
            assert abs(samples[0]) < 30 and abs(samples[-1]) < 30, f'Unfaded edge: {path}'
            checked+=1
    print(f'PASS {len(cues)} cues / {checked} clips / {len(original_hashes)} originals: coverage, CC0 provenance, hashes, encoding, fades and peak <= -3 dBFS')

if __name__ == '__main__':
    try: check()
    except (AssertionError,FileNotFoundError,KeyError) as error:
        print('FAIL', error)
        sys.exit(1)
