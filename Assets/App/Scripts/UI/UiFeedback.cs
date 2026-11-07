using App.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace App.UI
{
    public sealed class UiFeedback : MonoBehaviour
    {
        void Awake()=>GameAudio.Ensure();
        public static void Play(bool back)=>GameAudio.Play(back?SoundCue.UiBack:SoundCue.UiConfirm);
        public static void Invalid()=>GameAudio.Play(SoundCue.UiInvalid);
        public static void Activate(UnityAction action)
        {
            int before=GameAudio.InterfaceRequestVersion;
            action?.Invoke();
            // Back / ready / join own their cue, including when that bus is muted.
            if(before==GameAudio.InterfaceRequestVersion)Play(false);
        }
    }
}
