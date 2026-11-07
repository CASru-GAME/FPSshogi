using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using App.Main.Player;

namespace App.Audio
{
    /// <summary>Shared local-multiplayer mix. Gameplay has a separate pauseable voice pool.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        [Serializable] public sealed class CueDefinition
        {
            public string id;
            public bool ui;
            public float gain, interval, pitch;
            public string[] clips;
        }
        [Serializable] sealed class Catalog { public CueDefinition[] cues; }
        sealed class Cue
        {
            public CueDefinition definition;
            public AudioClip[] clips;
            public readonly int[] previous = { -1, -1, -1 };
            public readonly float[] last = { -1000, -1000, -1000 };
        }
        sealed class Voice
        {
            public AudioSource source;
            public Player owner;
            public bool ui, active;
            public AudioBus bus;
            public SoundCue cue;
            public bool resultTail;
            public double endDsp;
            public float remaining, gain, started;
        }
        public readonly struct Playback
        {
            public readonly SoundCue cue;
            public readonly string clip;
            public readonly int player;
            public readonly float pan;
            public Playback(SoundCue cue, string clip, int player, float pan)
            { this.cue=cue;this.clip=clip;this.player=player;this.pan=pan; }
        }
        public static GameAudio Instance { get; private set; }
        public static event Action<Playback> Played;
        public static int InterfaceRequestVersion { get; private set; }
        public static bool CombatPaused => Instance!=null && Instance.paused;
        public int LoadedCueCount => cues.Count;
        public int ActiveGameplayVoices => Count(false);
        public int ActiveInterfaceVoices => Count(true);
        readonly Dictionary<SoundCue,Cue> cues=new();
        readonly List<Voice> voices=new();
        readonly Player[] listeners=new Player[2];
        readonly System.Random random=new();
        bool paused;
        int lethalFrame=-1;
        AudioListener listener;
        static readonly string[] VolumeKeys={"FPSshogi.Audio.Master","FPSshogi.Audio.Effects","FPSshogi.Audio.Interface"};
        public static float DefaultVolume(AudioBus bus) => bus==AudioBus.Master?.8f:bus==AudioBus.Effects?.85f:.65f;
        public static float GetVolume(AudioBus bus) => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKeys[(int)bus],DefaultVolume(bus)));
        public static void SetVolume(AudioBus bus,float value)
        {
            PlayerPrefs.SetFloat(VolumeKeys[(int)bus],Mathf.Clamp01(value));
            if(Instance==null)return;
            if(value<=0) foreach(var voice in Instance.voices)
                if(bus==AudioBus.Master || voice.bus==bus) Instance.Stop(voice);
            Instance.ApplyVolumes();
        }
        public static void ResetVolumes()
        { foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))SetVolume(bus,DefaultVolume(bus)); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Instance=null;Played=null;InterfaceRequestVersion=0;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Ensure()
        { if(Application.isPlaying&&Instance==null)new GameObject("Game audio").AddComponent<GameAudio>(); }
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            var listenerObject=new GameObject("Shared audio listener");listenerObject.transform.SetParent(transform,false);
            listener=listenerObject.AddComponent<AudioListener>();
            var json=Resources.Load<TextAsset>("Audio/catalog");
            if(json==null){Debug.LogError("Audio catalog is missing.");return;}
            foreach(var definition in JsonUtility.FromJson<Catalog>(json.text).cues)
            {
                if(!Enum.TryParse(definition.id,out SoundCue id))continue;
                var clips=new List<AudioClip>();
                foreach(var path in definition.clips)
                {
                    var clip=Resources.Load<AudioClip>(path);
                    if(clip==null)Debug.LogError("Missing audio clip: "+path);else clips.Add(clip);
                }
                cues[id]=new Cue {definition=definition,clips=clips.ToArray()};
            }
            for(int i=0;i<32;i++)
            {
                bool ui=i<8;
                var source=gameObject.AddComponent<AudioSource>();
                source.playOnAwake=false;source.spatialBlend=0;source.ignoreListenerPause=ui;
                source.priority=ui?64:128;source.dopplerLevel=0;source.reverbZoneMix=0;
                voices.Add(new Voice {source=source,ui=ui});
            }
            SceneManager.sceneLoaded+=SceneLoaded;
            OwnListener();
        }
        void OwnListener()
        {
            // A single stable listener for split screen. Cameras may be disabled on the board.
            foreach(var other in FindObjectsByType<AudioListener>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(other!=listener)other.enabled=false;
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            foreach(var v in voices)if(!v.ui)Stop(v);
            lethalFrame=-1;paused=false;OwnListener();ApplyVolumes();
        }
        public static void Register(Player player)
        { if(Instance!=null&&player!=null)Instance.listeners[Slot(player)]=player; }
        public static int Slot(Player player)
        { return player!=null&&player.BodyLayer==LayerMask.NameToLayer("PlayerTwoBody")?1:0; }
        public static bool HasCue(SoundCue cue) => Instance!=null&&Instance.cues.TryGetValue(cue,out var c)&&c.clips.Length>0;
        public static bool Play(SoundCue id,Player owner=null,Vector3? position=null)
        {
            Ensure();return Instance!=null&&Instance.PlayInternal(id,owner,position);
        }
        public static bool PlayForSeat(SoundCue id,int seat)
        { Ensure();return Instance!=null&&Instance.PlayInternal(id,null,null,Mathf.Clamp(seat,0,1)); }
        public static void PreviewVolume(AudioBus bus)
        {
            Ensure();if(Instance==null)return;
            if(bus==AudioBus.Effects)Instance.PlayInternal(SoundCue.FootstepWood,null,null,2,AudioBus.Effects);
            else Play(SoundCue.UiNavigate);
        }
        public static void BulletContact(Player owner,RaycastHit hit)
        { if(hit.collider!=null&&hit.collider.GetComponentInParent<Player>()==null)Play(SoundCue.BulletImpact,owner,hit.point); }
        bool PlayInternal(SoundCue id,Player owner,Vector3? position,int seat=2,AudioBus? preview=null)
        {
            if(!cues.TryGetValue(id,out var cue)||cue.clips.Length==0)return false;
            var d=cue.definition;
            bool interfaceVoice=d.ui||preview.HasValue;
            var bus=preview??(d.ui?AudioBus.Interface:AudioBus.Effects);
            if(interfaceVoice)InterfaceRequestVersion++;
            if(!interfaceVoice&&(paused||Time.timeScale<=0))return false;
            if(GetVolume(AudioBus.Master)<=0||GetVolume(bus)<=0)return false;
            int slot=owner!=null?Slot(owner):seat;
            if(Time.unscaledTime-cue.last[slot]<d.interval)return false;
            if(id==SoundCue.Down)
            {
                // DuelManager resolves death in LateUpdate of this frame. Let the
                // contact and its shot finish on the effects bus during the result.
                lethalFrame=Time.frameCount;
                foreach(var v in voices)if(v.active&&!v.ui&&Time.unscaledTime-v.started<.22f&&IsStrike(v.cue))v.resultTail=true;
            }
            float gain=d.gain,spread=interfaceVoice?.18f:.32f,pan=slot==0?-spread:slot==1?spread:0;
            if(position.HasValue)
            {
                float left=Audibility(0,position.Value),right=Audibility(1,position.Value);
                if(left+right>0){gain*=Mathf.Max(left,right);pan=(right-left)/(right+left)*.45f;}
            }
            var voice=GetVoice(interfaceVoice);
            int index=cue.clips.Length==1?0:random.Next(cue.clips.Length-1);
            if(cue.clips.Length>1&&index>=cue.previous[slot])index++;
            // The first choice can use the entire set; subsequent choices never repeat immediately.
            if(cue.previous[slot]<0)index=random.Next(cue.clips.Length);
            cue.previous[slot]=index;cue.last[slot]=Time.unscaledTime;
            voice.source.Stop();voice.source.clip=cue.clips[index];voice.owner=owner;voice.bus=bus;voice.cue=id;
            voice.resultTail=id==SoundCue.Down||lethalFrame==Time.frameCount&&IsStrike(id);
            voice.source.pitch=1+(float)(random.NextDouble()*2-1)*d.pitch;
            voice.source.panStereo=pan;voice.gain=gain;voice.active=true;
            voice.started=Time.unscaledTime;voice.remaining=voice.source.clip.length/voice.source.pitch;
            voice.endDsp=AudioSettings.dspTime+voice.remaining;
            ApplyVolumes();voice.source.Play();Played?.Invoke(new Playback(id,voice.source.clip.name,slot,pan));
            return true;
        }
        static bool IsStrike(SoundCue id)=>id==SoundCue.ShortGun||id==SoundCue.LongGun||id==SoundCue.TurretShot||id==SoundCue.BowRelease||
            id==SoundCue.SpearSwing||id==SoundCue.SwordSwing||id==SoundCue.NaginataSwing||id==SoundCue.KnifeSwing||id==SoundCue.DashStrike||
            id==SoundCue.ArrowImpact||id==SoundCue.BulletImpact;
        float Audibility(int slot,Vector3 position)
        {
            var player=listeners[slot];if(player==null)return 0;
            float distance=Vector3.Distance(player.transform.position,position);
            return Mathf.Clamp01(1/(1+Mathf.Max(0,distance-4)*.045f));
        }
        Voice GetVoice(bool ui)
        {
            Voice oldest=null;
            foreach(var voice in voices)if(voice.ui==ui)
            { if(!voice.active)return voice;if(oldest==null||voice.started<oldest.started)oldest=voice; }
            return oldest;
        }
        void Update()
        {
            bool changed=false;
            foreach(var voice in voices)
            {
                if(!voice.active||!voice.ui&&!voice.resultTail&&paused)continue;
                // A render hitch before this voice started must not consume a short
                // transient. Reclaim only after the DSP clock and real source finish.
                if(AudioSettings.dspTime>=voice.endDsp&&!voice.source.isPlaying){Stop(voice);changed=true;}
            }
            if(changed)ApplyVolumes();
        }
        void ApplyVolumes()
        {
            float master=GetVolume(AudioBus.Master),effects=GetVolume(AudioBus.Effects),ui=GetVolume(AudioBus.Interface),sum=0;
            foreach(var v in voices)if(v.active&&(v.ui||v.resultTail||!paused))sum+=v.gain*master*(v.bus==AudioBus.Interface?ui:effects);
            // Every authored clip peaks <= .68. This conservative sum budget keeps overlapping
            // turrets + both players + UI below clipping, including sliders at maximum.
            float headroom=Mathf.Min(1,1.25f/Mathf.Max(.001f,sum));
            foreach(var v in voices)if(v.active)v.source.volume=v.gain*master*(v.bus==AudioBus.Interface?ui:effects)*headroom;
        }
        public static void SetCombatPaused(bool value)
        {
            if(Instance==null||Instance.paused==value)return;
            Instance.paused=value;
            foreach(var voice in Instance.voices)if(voice.active&&!voice.ui&&!voice.resultTail)
            {
                if(value){voice.remaining=(float)Math.Max(0,voice.endDsp-AudioSettings.dspTime);voice.source.Pause();}
                else{voice.endDsp=AudioSettings.dspTime+voice.remaining;voice.source.UnPause();}
            }
            Instance.ApplyVolumes();
        }
        public static void StopCombat(Player owner=null)
        {
            if(Instance==null)return;
            foreach(var voice in Instance.voices)if(!voice.ui&&!voice.resultTail&&(owner==null||voice.owner==owner))Instance.Stop(voice);
            Instance.ApplyVolumes();
        }
        void Stop(Voice voice){voice.source.Stop();voice.active=false;voice.resultTail=false;voice.owner=null;voice.remaining=0;}
        int Count(bool ui){int count=0;foreach(var v in voices)if(v.active&&v.ui==ui)count++;return count;}
        void OnApplicationQuit()=>PlayerPrefs.Save();
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;if(Instance==this)Instance=null;}
    }
}
