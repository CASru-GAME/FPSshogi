using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App.Audio
{
    public sealed class UiFocusSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        float created;
        void OnEnable()=>created=Time.unscaledTime;
        public void OnSelect(BaseEventData data){if(!(data is PointerEventData))Focus();}
        public void OnPointerEnter(PointerEventData data)=>Focus();
        void Focus()
        {
            if(Time.unscaledTime-created<.2f||!TryGetComponent<Selectable>(out var selectable)||!selectable.IsInteractable())return;
            GameAudio.Play(SoundCue.UiNavigate);
        }
    }
}
