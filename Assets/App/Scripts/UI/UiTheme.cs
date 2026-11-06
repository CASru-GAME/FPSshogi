using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace App.UI
{
    public static class UiTheme
    {
        public static readonly Color Ink = new Color32(30, 24, 20, 245);
        public static readonly Color Panel = new Color32(48, 37, 29, 242);
        public static readonly Color Paper = new Color32(247, 234, 208, 255);
        public static readonly Color Muted = new Color32(196, 178, 150, 255);
        public static readonly Color Gold = new Color32(238, 186, 94, 255);
        public static readonly Color Blue = new Color32(103, 218, 223, 255);
        static Font font;
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
            text.font = font; text.text = value; text.fontSize = size; text.color = color ?? Paper;
            text.alignment = align; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        public static Button Button(Transform parent, string name, string caption, float x, float y, float w, float h, UnityAction action, bool primary = false)
        {
            var box = Box(parent, name, x, y, w, h, primary ? Gold : Panel, true);
            var button = box.gameObject.AddComponent<Button>(); button.targetGraphic = box;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = new Color(1.3f, 1.3f, 1.3f); colors.pressedColor = new Color(.7f, .7f, .7f);
            colors.disabledColor = new Color(.5f, .5f, .5f, .65f); colors.fadeDuration = .1f; button.colors = colors;
            var outline = box.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, .6f); outline.effectDistance = new Vector2(1, -1);
            Label(box.transform, "Label", caption, 14, 4, w-28, h-8, 27, primary ? Ink : Paper, TextAnchor.MiddleCenter);
            button.onClick.AddListener(action); return button;
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
