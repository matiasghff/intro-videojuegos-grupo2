using UnityEngine;
using UnityEngine.UI;

namespace UltimaJugada
{
    /// <summary>Screen-space ball target; clicks outside its actual circle are ignored.</summary>
    public sealed class PrecisionController : System.IDisposable
    {
        public bool Active { get; private set; }
        public float Remaining { get; private set; }
        public RectTransform Target { get; private set; }
        private readonly GameObject root;
        private readonly Text timer;
        private readonly Texture2D texture;
        private readonly Sprite sprite;

        public PrecisionController(Canvas canvas)
        {
            var panel = UIFactory.Panel(canvas.transform, "Precisión · balón", new Vector2(0.20f, 0.33f), new Vector2(0.60f, 0.72f), UIFactory.Surface);
            root = panel.gameObject;
            UIFactory.Label(panel.transform, "PRECISIÓN", new Vector2(0.04f, 0.77f), new Vector2(0.96f, 0.98f), 32, UIFactory.Accent, TextAnchor.MiddleCenter);
            timer = UIFactory.Label(panel.transform, "", new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.18f), 23, null, TextAnchor.MiddleCenter);
            Target = UIFactory.Rect("Haz clic en el centro del balón", panel.transform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f));
            Target.sizeDelta = new Vector2(210, 210);
            texture = CreateBallTarget();
            sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
            var image = Target.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            root.SetActive(false);
        }
        public void Begin(float timeLimit)
        {
            Remaining = timeLimit;
            Active = true;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Target.anchoredPosition = new Vector2(Random.Range(-135f, 135f), Random.Range(-12f, 12f));
            Canvas.ForceUpdateCanvases();
            UpdateTimer();
        }
        public int? Tick(float deltaTime)
        {
            if (!Active) return null;
            Remaining = Mathf.Max(0f, Remaining - deltaTime);
            UpdateTimer();
            // Time expiry wins over late clicks.
            if (Remaining <= 0f) return Complete(0);
            if (GameInput.Clicked && RectTransformUtility.ScreenPointToLocalPointInRectangle(Target, GameInput.MousePosition, null, out var local)
                && local.magnitude <= Target.rect.width * 0.5f)
                return Complete(Ballistics.PrecisionScore(local, Target.rect.width * 0.5f));
            return null;
        }
        private void UpdateTimer() => timer.text = $"Clic en el centro   ·   {Remaining:0.0} s";
        private int Complete(int score) { Active = false; root.SetActive(false); return score; }
        private static Texture2D CreateBallTarget()
        {
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false) { name = "Balón de precisión procedural" };
            var pixels = new Color[256 * 256];
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                Vector2 p = new Vector2(x - 127.5f, y - 127.5f);
                float r = p.magnitude;
                Color c = new Color(0.94f, 0.96f, 0.88f);
                if (r > 125f) c = Color.clear;
                else if (r > 119f || Mathf.Abs(r - 40f) < 1.5f || Mathf.Abs(r - 80f) < 1.5f) c = UIFactory.Accent;
                else if (r < 6f) c = UIFactory.Accent;
                else if (r < 22f || (r > 91f && Mathf.Cos(Mathf.Atan2(p.y, p.x) * 5f) > 0.5f)) c = UIFactory.Background;
                pixels[y * 256 + x] = c;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        public void Dispose()
        {
            Object.Destroy(root);
            Object.Destroy(sprite);
            Object.Destroy(texture);
        }
    }
}
