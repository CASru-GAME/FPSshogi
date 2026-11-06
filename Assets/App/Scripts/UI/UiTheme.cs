using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace App.UI
{
    public static class UiTheme
    {
        public static readonly Color Ink = new Color32(32, 36, 29, 255);
        public static readonly Color Panel = new Color32(237, 238, 229, 255);
        public static readonly Color Paper = new Color32(247, 246, 236, 255);
        public static readonly Color Muted = new Color32(80, 87, 73, 255);
        public static readonly Color Gold = new Color32(64, 104, 53, 255);
        public static readonly Color Blue = new Color32(135, 88, 41, 255);
        public static readonly Color Track = new Color32(196, 204, 185, 255);
        public static readonly Color FocusColor = new Color32(214, 224, 201, 255);
        static Font font, titleFont;
        public static Color PlayerColor(int player) => player==0?Gold:Blue;
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        public static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color, bool hit = false)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = hit; return image;
        }
        public static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size = 26, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            if (font == null) font = Resources.Load<Font>("UI/NotoSansCJKjp-Regular");
            var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color ?? Ink;
            text.alignment = align; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        public static Button Button(Transform parent, string name, string caption, float x, float y, float w, float h, UnityAction action, bool primary = false)
        {
            var box = Box(parent, name, x, y, w, h, Color.white, true);
            var button = box.gameObject.AddComponent<Button>(); button.targetGraphic = box;
            var colors = button.colors;
            colors.normalColor = primary?Ink:Panel; colors.highlightedColor = primary?Gold:FocusColor;
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = primary?Muted:Track;
            colors.disabledColor = new Color(Panel.r,Panel.g,Panel.b,.45f); colors.fadeDuration = .08f; button.colors = colors;
            Label(box.transform, "Label", caption, 16, 4, w-32, h-8, 26, primary ? Paper : Ink, TextAnchor.MiddleCenter);
            button.onClick.AddListener(()=>{UiFeedback.Play(false);action?.Invoke();}); return button;
        }
        public static Text Heading(Transform parent,string name,string value,float x,float y,float w,float h,int size)
        {
            var text=Label(parent,name,value,x,y,w,h,size);
            if(titleFont==null) titleFont=Font.CreateDynamicFontFromOSFont(new[]{"Yu Mincho","MS Mincho","Noto Serif CJK JP"},size);
            if(titleFont!=null)text.font=titleFont;
            return text;
        }
        public static UiSymbol Symbol(Transform parent,string name,UiSymbol.Kind kind,float x,float y,float size,Color? color=null)
        {
            var icon=Rect(parent,name,x,y,size,size).gameObject.AddComponent<UiSymbol>();
            icon.Shape=kind;icon.color=color??Ink;icon.raycastTarget=false;return icon;
        }
        public static Text Key(Transform parent,string name,string value,float x,float y,float w=72,float h=34)
        {
            var box=Box(parent,name+" key",x,y,w,h,Track);
            return Label(box.transform,name,value,3,1,w-6,h-2,h<32?18:20,Ink,TextAnchor.MiddleCenter);
        }
        public static RawImage Portrait(Transform parent,string name,Texture texture,float x,float y,float size)
        {
            var image=Rect(parent,name,x,y,size,size).gameObject.AddComponent<RawImage>();
            image.texture=texture;image.raycastTarget=false;return image;
        }
        public static void VerticalNavigation(params Selectable[] controls)
        {
            for(int i=0;i<controls.Length;i++)controls[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=controls[(i+controls.Length-1)%controls.Length],selectOnDown=controls[(i+1)%controls.Length]};
        }
        public static RectTransform Frame(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name, 0, 0, 70, 70);
            Box(rt, "Top", 0, 0, 70, 4, color);
            Box(rt, "Bottom", 0, 66, 70, 4, color);
            Box(rt, "Left", 0, 0, 4, 70, color);
            Box(rt, "Right", 66, 0, 4, 70, color);
            return rt;
        }
    }
}
