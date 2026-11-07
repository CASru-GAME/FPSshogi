"""Rebuild the authored CC0 cues. Requires numpy, scipy, soundfile and ffmpeg.

Original selected recordings live in AudioSources/raw; runtime WAVs are mono
44.1 kHz PCM16. First preparation can read the downloaded packs under
.utmp/audio/source. No network access is performed by this script.
"""
from pathlib import Path
import hashlib
import json
import math
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.utmp/audio/python'))
sys.path.insert(0, str(ROOT / '.utmp/motion-repair/python'))
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfilt, resample_poly

RATE = 44100
RAW = ROOT / 'AudioSources/raw'
OUT = ROOT / 'Assets/App/Resources/Audio'
PACKS = ROOT / '.utmp/audio/source'
SOURCES = {
    'impact': ('Kenney', 'https://kenney.nl/assets/impact-sounds', 'impact-sounds/Audio'),
    'rpg': ('Kenney', 'https://kenney.nl/assets/rpg-audio', 'rpg-audio/Audio'),
    'swish': ('artisticdude', 'https://opengameart.org/content/swishes-sound-pack', 'swishes/swishes'),
    'gun': ('kurt', 'https://opengameart.org/content/gunshots', ''),
    'bow': ('Hanbaal', 'https://freesound.org/people/Hanbaal/sounds/178873/', ''),
}
manifest = {'license': 'CC0-1.0', 'processing': 'mono 44100 Hz; trim; DC removal; high-pass 45 Hz; optional low-pass/pitch/layers; 2 ms attack and 25 ms release fades; peak at most -3.35 dBFS', 'clips': {}}
catalog = []
cache = {}

def source(group, filename, speed=1., start=None, end=None, lowpass=None):
    author, url, folder = SOURCES[group]
    dest = RAW / group / filename
    if not dest.exists():
        origin = PACKS / folder / filename
        if group == 'gun': origin = ROOT / '.utmp/audio-black-powder.wav'
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(origin, dest)
    if dest not in cache:
        if dest.suffix == '.mp3':
            import imageio_ffmpeg
            data = subprocess.check_output([imageio_ffmpeg.get_ffmpeg_exe(), '-v','error','-i',str(dest),'-f','f32le','-ac','1','-ar',str(RATE),'-'])
            x=np.frombuffer(data, dtype='<f4').astype(float)
        else:
            x,sr = sf.read(dest, always_2d=True)
            x=x.mean(axis=1)
            if sr!=RATE:
                g=math.gcd(sr,RATE); x=resample_poly(x,RATE//g,sr//g)
        cache[dest]=x
    x=cache[dest].copy()
    if start is not None or end is not None: x=x[int((start or 0)*RATE):int(end*RATE) if end else None]
    x-=x.mean()
    active=np.flatnonzero(np.abs(x)>max(.001,np.max(abs(x))*.006))
    if len(active): x=x[max(0,active[0]-int(.003*RATE)):min(len(x),active[-1]+int(.025*RATE))]
    if speed!=1: x=np.interp(np.arange(0,len(x),speed),np.arange(len(x)),x)
    x=sosfilt(butter(2,45,fs=RATE,btype='highpass',output='sos'),x)
    if lowpass: x=sosfilt(butter(2,lowpass,fs=RATE,btype='lowpass',output='sos'),x)
    x=x / max(.0001,np.max(abs(x))) * .65
    record={'author':author,'url':url,'license':'CC0-1.0','file':dest.relative_to(ROOT).as_posix(),
            'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'speed':speed,'start':start,'end':end,'lowpass':lowpass}
    return x,[record]

def mix(*layers):
    # Each layer is (prepared source, offset seconds, linear gain).
    length=max(len(s[0])+int(t*RATE) for s,t,g in layers)
    x=np.zeros(length); records=[]
    for (samples,provenance),time,gain in layers:
        offset=int(time*RATE); x[offset:offset+len(samples)]+=samples*gain
        records.extend(dict(p,delay=time,layerGain=gain) for p in provenance)
    return x,records

def cue(name, variants, gain=.35, ui=False, interval=.06, pitch=.025):
    names=[]
    for i,(x,sources) in enumerate(variants):
        x=x.copy()
        if len(x)>RATE*3.5: x=x[:int(RATE*3.5)]
        # Peak-safe normalization; no aggressive compression of transients.
        x*=min(.68/max(.00001,np.max(abs(x))), .12/max(.00001,np.sqrt(np.mean(x*x))))
        fade=min(int(.025*RATE),len(x)//4); attack=min(int(.002*RATE),len(x)//8)
        x[:attack]*=np.linspace(0,1,attack); x[-fade:]*=np.linspace(1,0,fade)
        stem=f'{name}_{i+1:02d}'
        file=OUT/'Clips'/f'{stem}.wav'; file.parent.mkdir(parents=True,exist_ok=True)
        sf.write(file,x,RATE,subtype='PCM_16')
        names.append('Audio/Clips/'+stem)
        manifest['clips'][stem]={'sources':sources,'seconds':round(len(x)/RATE,4),'peak':round(float(max(abs(x))),5),'sha256':hashlib.sha256(file.read_bytes()).hexdigest()}
    catalog.append(dict(id=name,ui=ui,gain=gain,interval=interval,pitch=pitch,clips=names))

def impact(kind,n=0,**kwargs): return source('impact',f'{kind}_{n:03d}.ogg',**kwargs)
def rpg(name,**kwargs): return source('rpg',name+'.ogg',**kwargs)
def swish(n=1,**kwargs): return source('swish',f'swish-{n}.wav',**kwargs)
def bell(speed=1.): return impact('impactBell_heavy',0,speed=speed,lowpass=6000)
def sequence(speeds): return mix(*[(bell(s),i*.17,.6) for i,s in enumerate(speeds)])

def build():
    # Board/UI: short wood contact and paper/cloth instead of sine tones.
    cue('UiNavigate',[impact('impactWood_light',i,lowpass=2600) for i in (0,2,4)],.16,True,.075)
    cue('UiConfirm',[mix((impact('impactWood_light',1),0,1),(rpg('cloth1'),.015,.15))],.30,True,.06,0)
    cue('UiBack',[rpg('bookClose',speed=1.3,lowpass=3500)],.24,True,.08,0)
    cue('UiInvalid',[mix((impact('impactWood_light',3,speed=.8),0,.7),(impact('impactWood_light',3,speed=.7),.09,.6))],.25,True,.22,0)
    cue('UiOpen',[rpg('bookOpen',speed=1.35,lowpass=3200)],.20,True,.10)
    cue('PlayerJoin',[mix((impact('impactWood_medium',1),0,.8),(bell(1.5),.045,.2))],.36,True,.10,0)
    cue('PlayerReady',[sequence([1.,1.25])],.39,True,.10,0)
    cue('PlayerLeave',[rpg('bookClose',speed=.9,lowpass=3500)],.28,True,.10,0)
    cue('Disconnect',[sequence([1.,.8])],.40,True,.5,0)
    cue('BoardCursor',[impact('impactWood_light',i,lowpass=2400) for i in (0,4)],.13,True,.055)
    cue('BoardSelect',[impact('impactWood_light',1)],.28,True,.06)
    cue('BoardCancel',[rpg('bookFlip1',speed=1.35,lowpass=3000)],.22,True,.12)
    cue('BoardMove',[impact('impactWood_medium',i,lowpass=6000) for i in (0,2,4)],.45,True,.08)
    cue('BoardDrop',[mix((impact('impactWood_medium',i),0,1),(impact('impactWood_light',1),.045,.3)) for i in (1,3)],.48,True,.08)
    cue('Promote',[mix((rpg('bookFlip2'),0,.25),(bell(1.25),.07,.6))],.35,True,.2,0)
    cue('Countdown',[impact('impactWood_heavy',1,lowpass=3000)],.38,True,.45,0)
    cue('DuelStart',[mix((impact('impactWood_heavy',0,speed=.8),0,.6),(bell(.7),0,.6))],.52,True,.4,0)
    cue('DuelWin',[sequence([1.,1.25])],.42,True,.4,0)
    cue('DuelDraw',[bell(.84)],.36,True,.4,0)
    cue('MatchWin',[sequence([1.,1.25,1.5])],.48,True,.5,0)
    # Actual weapon recordings; lighter/thinner swings for spear and knife.
    cue('SpearSwing',[mix((swish(i,speed=.8),0,1),(rpg('cloth2'),.015,.2)) for i in (1,2,3)],.45)
    cue('SwordSwing',[mix((swish(i,speed=.78),0,1),(rpg('knifeSlice'),.025,.3)) for i in (6,7,9)],.48)
    cue('NaginataSwing',[mix((swish(i,speed=.62),0,1),(rpg('cloth4'),.015,.2)) for i in (8,11,13)],.49)
    cue('KnifeSwing',[swish(i,speed=1.25) for i in (1,3,4)],.32)
    shots=[source('gun','Black Powder.wav',start=p-.025,end=p+.48) for p in (.14,.84,1.55)]
    cue('ShortGun',shots,.68,interval=.07,pitch=.035)
    cue('LongGun',[mix((source('gun','Black Powder.wav',speed=.88,start=p-.025,end=p+.52),0,1),(impact('impactSoft_heavy',0,speed=.7),.005,.4)) for p in (2.26,2.94,3.50)],.70,interval=.07,pitch=.025)
    cue('BowRelease',[source('bow','bow2.mp3',speed=s) for s in (1.,.97,1.04)],.49,interval=.08,pitch=.015)
    cue('BowNock',[mix((rpg('creak1',speed=1.5),0,.5),(rpg('clothBelt'),.04,.25))],.25,interval=.12)
    cue('GuardRaise',[mix((rpg('drawKnife1',speed=1.3),0,.5),(rpg('cloth2'),0,.4))],.37)
    cue('GuardEnd',[rpg('cloth3',speed=1.25)],.18)
    cue('AimIn',[rpg('handleSmallLeather',speed=1.4)],.21,interval=.08)
    cue('AimOut',[rpg('handleSmallLeather2',speed=1.2)],.18,interval=.08)
    cue('Leap',[mix((impact('footstep_carpet',2),0,.3),(swish(12,speed=.6),0,.8))],.46)
    cue('Throw',[mix((rpg('metalClick'),0,.35),(swish(4,speed=.8),.06,1))],.39)
    cue('FlashBurst',[mix((shots[1],0,.7),(impact('impactMetal_heavy',0,speed=.65,lowpass=5000),.025,.3))],.59,interval=.12,pitch=0)
    cue('Dash',[mix((swish(11,speed=.6),0,1),(rpg('cloth4'),0,.2))],.47)
    cue('DashStrike',[mix((swish(9,speed=.7),0,1),(rpg('knifeSlice2'),.02,.4))],.53)
    cue('GinShield',[mix((bell(1.2),0,.8),(rpg('drawKnife2'),.03,.3))],.45,interval=.1,pitch=0)
    cue('KinShield',[mix((bell(.8),0,.8),(impact('impactMetal_medium',0,speed=.8),.03,.3))],.46,interval=.1,pitch=0)
    cue('BuffEnd',[bell(1.6)],.18,interval=.1,pitch=0)
    cue('Ascend',[mix((swish(13,speed=.45),0,1),(bell(1.3),.06,.25))],.47)
    cue('FieldCast',[mix((bell(.5),0,.7),(swish(8,speed=.55),0,.45))],.48,interval=.1,pitch=0)
    cue('FieldEnter',[rpg('clothBelt2',speed=.7,lowpass=2000)],.24,interval=.3)
    cue('FieldExit',[rpg('cloth1',speed=1.2)],.18,interval=.3)
    cue('Command',[mix((rpg('cloth4'),0,.6),(impact('impactWood_heavy',0),.08,.55),(bell(.85),.1,.3))],.49,interval=.1,pitch=0)
    cue('TurretSpawn',[mix((impact('impactWood_heavy',3),0,.6),(rpg('metalLatch'),.035,.5))],.33,interval=.08)
    cue('TurretShot',[source('gun','Black Powder.wav',speed=1.12,start=p-.025,end=p+.4,lowpass=7000) for p in (.84,1.55,2.26)],.40,interval=.035)
    cue('TurretEnd',[mix((rpg('metalLatch',speed=.8),0,.5),(impact('impactWood_light',2),.07,.7))],.24,interval=.1)
    cue('Hit',[impact('impactPunch_heavy',i,lowpass=6000) for i in (0,2,4)],.49,interval=.045)
    cue('Block',[impact('impactMetal_medium',i,lowpass=6500) for i in (0,2,4)],.46,interval=.06)
    cue('Down',[mix((impact('impactSoft_heavy',1,speed=.75),0,1),(rpg('dropLeather'),.05,.4))],.45,interval=.2)
    cue('ArrowImpact',[mix((impact('impactWood_light',i),0,.8),(rpg('cloth2',speed=1.5),0,.2)) for i in (1,3)],.31,interval=.06)
    cue('BulletImpact',[impact('impactPlank_medium',i,lowpass=6500) for i in (0,2,4)],.23,interval=.04)
    cue('FootstepSoft',[impact('footstep_carpet',i,lowpass=4200) for i in range(5)],.24,interval=.16)
    cue('FootstepWood',[impact('footstep_wood',i,lowpass=5000) for i in range(5)],.25,interval=.16)
    cue('Land',[mix((impact('impactSoft_heavy',i),0,.6),(impact('footstep_carpet',i),0,.7)) for i in (0,2)],.36,interval=.2)
    cue('Climb',[mix((rpg('cloth2'),0,.5),(impact('impactWood_light',i),.04,.5)) for i in (1,3)],.20,interval=.32)
    cue('SkillReady',[bell(1.8)],.14,interval=.5,pitch=0)
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'catalog.json').write_text(json.dumps({'cues':catalog},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (ROOT/'AudioSources/manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    from audio_audition import write
    write(ROOT, catalog)
    print(f'Prepared {len(catalog)} cues, {len(manifest["clips"])} WAVs from {len(cache)} selected recordings.')

if __name__=='__main__': build()
