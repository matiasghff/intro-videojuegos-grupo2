using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UltimaJugada
{
    public static class UIFactory
    {
        public static readonly Color Background = new Color(0.025f, 0.045f, 0.075f);
        public static readonly Color Surface = new Color(0.055f, 0.095f, 0.14f);
        public static readonly Color TextColor = new Color(0.91f, 0.95f, 0.90f);
        public static readonly Color Muted = new Color(0.58f, 0.70f, 0.79f);
        public static readonly Color Accent = new Color(0.67f, 0.94f, 0.39f);
        public static readonly Color Warning = new Color(1f, 0.48f, 0.30f);
        private static Font font;

        public static Canvas Canvas(Transform parent)
        {
            var go = new GameObject("UI · Ultima Jugada", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(parent, false);
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }
        public static Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var rt = Rect(name, parent, min, max);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
        public static Image Card(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var panel = Panel(parent, name, min, max, Surface);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.20f, 0.32f, 0.40f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);
            Panel(panel.transform, "Acento", new Vector2(0f, 0.99f), Vector2.one, Accent);
            return panel;
        }
        public static Text Label(Transform parent, string text, Vector2 min, Vector2 max, int size = 26, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rt = Rect(text.Length > 28 ? text.Substring(0, 28) : text, parent, min, max);
            var label = rt.gameObject.AddComponent<Text>();
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color ?? TextColor;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.supportRichText = false;
            return label;
        }
        public static Button Button(Transform parent, string text, Vector2 min, Vector2 max, Action onClick, bool primary = true)
        {
            var image = Panel(parent, text, min, max, primary ? Accent : new Color(0.12f, 0.20f, 0.28f));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.84f, 1f, 0.68f);
            colors.pressedColor = new Color(0.56f, 0.73f, 0.46f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = Label(image.transform, text, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), 27, primary ? Background : TextColor, TextAnchor.MiddleCenter);
            label.gameObject.name = "Texto del boton";
            button.onClick.AddListener(() => onClick());
            return button;
        }
        public static Image Meter(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var back = Panel(parent, name, min, max, new Color(0.12f, 0.22f, 0.18f));
            return Panel(back.transform, "Valor", Vector2.zero, Vector2.one, color);
        }
        public static void SetMeter(Image image, float value, Color color)
        {
            image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
            image.color = color;
        }
    }
}
