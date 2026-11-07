using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace App.UI
{
    public static class UiTheme
    {
        public static readonly Color Ink = new Color32(247, 245, 234, 255);
        public static readonly Color Muted = new Color32(200, 201, 191, 255);
        public static readonly Color Gold = new Color32(165, 203, 150, 255);
        public static readonly Color Blue = new Color32(234, 183, 116, 255);
        public static readonly Color Shadow = new Color32(12, 17, 16, 255);
        public static readonly Color Track = new Color32(105, 114, 105, 255);
        public static readonly Color Rule = new Color32(228, 230, 214, 65);
        public static readonly Color FocusColor = new Color32(234, 235, 215, 35);
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
        public static RectTransform Shade(Transform parent,string name,float x,float y,float w,float h,UiSymbol.Kind edge,float opacity=.85f,float solid=0)
        {
            var graphic=Rect(parent,name,x,y,w,h).gameObject.AddComponent<UiSymbol>();
            graphic.Shape=edge;graphic.Solid=solid;graphic.color=new Color(Shadow.r,Shadow.g,Shadow.b,opacity);graphic.raycastTarget=false;return graphic.rectTransform;
        }
        public static RectTransform EdgeShade(Transform parent,string name,float x,float y,float w,float h,UiSymbol.Kind edge,float opacity=.85f,float solid=0)
        {
            // Extend only the solid side and the perpendicular edges. The fade stays
            // at the same content coordinates even when the canvas expands past 16:9.
            const float margin=4096;
            if(edge==UiSymbol.Kind.LeftShade||edge==UiSymbol.Kind.RightShade){
                solid=(margin+w*solid)/(margin+w);
                if(edge==UiSymbol.Kind.LeftShade)x-=margin;
                w+=margin;y-=margin;h+=margin*2;
            }else{
                solid=(margin+h*solid)/(margin+h);
                if(edge==UiSymbol.Kind.TopShade)y-=margin;
                h+=margin;x-=margin;w+=margin*2;
            }
            return Shade(parent,name,x,y,w,h,edge,opacity,solid);
        }
        public static void ScreenDim(Transform parent,float opacity)
        {
            // The layout is centered at 1920x1080; overscan also covers wider/taller canvases.
            Box(parent,"Screen dim",-4096,-4096,10112,9272,new Color(Shadow.r,Shadow.g,Shadow.b,opacity),true);
        }
        public static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size = 26, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            if (font == null) font = Resources.Load<Font>("UI/NotoSansCJKjp-Regular");
            var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color ?? Ink;
            text.alignment = align; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            var shadow=text.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(0,0,0,.65f);shadow.effectDistance=new Vector2(0,-1.5f);
            return text;
        }
        public static Button Button(Transform parent, string name, string caption, float x, float y, float w, float h, UnityAction action, bool primary = false)
        {
            var box = Box(parent, name, x, y, w, h, Color.white, true);
            var button = box.gameObject.AddComponent<UiMenuButton>(); button.targetGraphic = box;
            var colors = button.colors;
            colors.normalColor = Color.clear; colors.highlightedColor = FocusColor;
            colors.selectedColor = FocusColor; colors.pressedColor = new Color(1,1,1,.22f);
            colors.disabledColor = Color.clear; colors.fadeDuration = .08f; button.colors = colors;
            button.Caption=Label(box.transform, "Label", caption, 16, 4, w-32, h-8, 26, Ink, TextAnchor.MiddleCenter);
            button.FocusLine=Box(box.transform,"Focus line",16,h-3,w-32,2,Ink);
            button.SetCurrent(primary);
            box.gameObject.AddComponent<App.Audio.UiFocusSound>();
            button.onClick.AddListener(()=>UiFeedback.Activate(action)); return button;
        }
        public static void Current(Button button,bool current)
        {
            if(button is UiMenuButton menuButton)menuButton.SetCurrent(current);
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
            var key=Rect(parent,name+" key",x,y,w,h);
            Box(key,"Key underline",3,h-1,w-6,1,Rule);
            return Label(key,name,value,3,1,w-6,h-2,h<32?18:20,Ink,TextAnchor.MiddleCenter);
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

    // Uses the same mouse/controller selection states and hit area as a normal Button.
    public sealed class UiMenuButton : Button
    {
        public Text Caption;
        public Image FocusLine;
        bool current;
        public void SetCurrent(bool value){current=value;RefreshAppearance();}
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            base.DoStateTransition(state,instant);RefreshAppearance();
        }
        void RefreshAppearance()
        {
            bool enabled=IsInteractable();
            bool focus=currentSelectionState==SelectionState.Selected||currentSelectionState==SelectionState.Highlighted||currentSelectionState==SelectionState.Pressed;
            if(Caption!=null)Caption.color=enabled?UiTheme.Ink:UiTheme.Muted;
            if(FocusLine!=null){FocusLine.gameObject.SetActive(enabled&&(current||focus));FocusLine.color=focus?UiTheme.Ink:UiTheme.Gold;}
        }
    }
}
