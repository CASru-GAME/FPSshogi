using UnityEngine;

namespace App.UI
{
    public sealed class UiFeedback : MonoBehaviour
    {
        static UiFeedback instance;
        AudioSource source;
        AudioClip confirm,cancel,invalid,hit,guard;
        readonly AudioSource[] combat=new AudioSource[2];
        void Awake(){
            instance=this;source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.ignoreListenerPause=true;source.volume=.13f;
            confirm=Tone(620);cancel=Tone(340);invalid=Tone(160);hit=Tone(190);guard=Tone(1100);
            for(int p=0;p<2;p++){combat[p]=gameObject.AddComponent<AudioSource>();combat[p].playOnAwake=false;combat[p].spatialBlend=0;combat[p].panStereo=p==0?-.65f:.65f;combat[p].volume=.16f;}
        }
        static AudioClip Tone(float hz){const int count=2205;var data=new float[count];for(int i=0;i<count;i++){float t=i/22050f;data[i]=Mathf.Sin(t*hz*Mathf.PI*2)*Mathf.Exp(-t*65)*Mathf.Min(1,i/44f);}var clip=AudioClip.Create("UI tick",count,1,22050,false);clip.SetData(data,0);return clip;}
        public static void Play(bool back){if(instance!=null)instance.source.PlayOneShot(back?instance.cancel:instance.confirm);}
        public static void Invalid(){if(instance!=null)instance.source.PlayOneShot(instance.invalid);}
        public static void Impact(int player,bool blocked){if(instance!=null)instance.combat[player].PlayOneShot(blocked?instance.guard:instance.hit);}
        void OnDestroy(){if(instance==this)instance=null;Destroy(confirm);Destroy(cancel);Destroy(invalid);Destroy(hit);Destroy(guard);}
    }
}
