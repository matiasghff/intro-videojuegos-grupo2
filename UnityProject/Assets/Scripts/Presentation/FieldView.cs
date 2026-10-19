using UnityEngine;

namespace UltimaJugada
{
    /// <summary>Original procedural art, shared by the scene generator and runtime actors.</summary>
    public sealed class FieldView : MonoBehaviour
    {
        public const float HalfWidth = 5.5f;
        public const float Bottom = -9f;
        public const float Top = 9f;
        public const float GoalLine = 7.7f;
        public const float GoalHalfWidth = 2.1f;
        public Camera FieldCamera { get; private set; }
        public Transform Actors { get; private set; }
        private Material blue, red, white, gold;
        private readonly System.Collections.Generic.List<Material> ownedMaterials = new System.Collections.Generic.List<Material>();

        public void Initialize()
        {
            FieldCamera = GetComponentInChildren<Camera>();
            Actors = new GameObject("Interactive actors").transform;
            Actors.SetParent(transform, false);
            blue = Material("Local · azul", new Color(0.18f, 0.60f, 1f));
            red = Material("Rival · coral", new Color(1f, 0.25f, 0.25f));
            white = Material("Balón", Color.white);
            gold = Material("Objetivo", new Color(1f, 0.79f, 0.25f));
        }

        public static FieldView Build(Transform parent)
        {
            var root = new GameObject("Cancha · vista superior");
            root.transform.SetParent(parent, false);
            var field = root.AddComponent<FieldView>();
            var cameraObject = new GameObject("Field Camera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 25, 0), Quaternion.Euler(90, 0, 0));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10.8f;
            camera.rect = new Rect(0.015f, 0.15f, 0.70f, 0.69f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.085f, 0.075f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60f;

            var grassA = field.Material("Cesped claro", new Color(0.10f, 0.36f, 0.22f));
            var grassB = field.Material("Cesped oscuro", new Color(0.075f, 0.29f, 0.18f));
            var line = field.Material("Marcas", new Color(0.75f, 0.88f, 0.78f));
            var net = field.Material("Red", new Color(0.38f, 0.49f, 0.45f));
            for (int i = 0; i < 12; i++)
                field.Cube("Franja de césped", new Vector3(0, -0.15f, -8.25f + i * 1.5f), new Vector3(11.7f, 0.2f, 1.5f), i % 2 == 0 ? grassA : grassB, root.transform);
            field.Mark(new Vector3(-HalfWidth, 0.02f, 0), new Vector3(0.055f, 0.02f, 18), line);
            field.Mark(new Vector3(HalfWidth, 0.02f, 0), new Vector3(0.055f, 0.02f, 18), line);
            foreach (float z in new[] { Bottom, 0f, Top })
                field.Mark(new Vector3(0, 0.02f, z), new Vector3(11, 0.02f, 0.055f), line);
            field.Circle("Círculo central", Vector3.zero, 1.6f, line, root.transform);
            foreach (int sign in new[] { -1, 1 })
            {
                field.Mark(new Vector3(0, 0.02f, sign * 5.5f), new Vector3(7f, 0.02f, 0.055f), line);
                field.Mark(new Vector3(-3.5f, 0.02f, sign * 7.25f), new Vector3(0.055f, 0.02f, 3.5f), line);
                field.Mark(new Vector3(3.5f, 0.02f, sign * 7.25f), new Vector3(0.055f, 0.02f, 3.5f), line);
            }
            // The playable goal is inset from the touchline, with a visible scoring line.
            field.Cube("Línea de gol", new Vector3(0, 0.04f, GoalLine), new Vector3(4.2f, 0.04f, 0.08f), line, root.transform);
            foreach (float x in new[] { -GoalHalfWidth, GoalHalfWidth })
                field.Cube("Poste", new Vector3(x, 0.65f, GoalLine), new Vector3(0.14f, 1.3f, 0.14f), line, root.transform);
            field.Cube("Travesaño", new Vector3(0, 1.25f, GoalLine), new Vector3(4.3f, 0.12f, 0.12f), line, root.transform);
            for (int i = 0; i <= 10; i++)
                field.Cube("Red vertical", new Vector3(-2.1f + i * 0.42f, 0.15f, 8.35f), new Vector3(0.025f, 0.025f, 1.3f), net, root.transform);
            for (int i = 0; i <= 4; i++)
                field.Cube("Red horizontal", new Vector3(0, 0.15f, GoalLine + i * 0.32f), new Vector3(4.2f, 0.025f, 0.025f), net, root.transform);
            return field;
        }

        public Transform Player(string name, Vector3 position, bool opponent = false)
        {
            var actor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            actor.name = name;
            actor.transform.SetParent(Actors, false);
            actor.transform.position = position;
            actor.transform.localScale = new Vector3(0.62f, 0.5f, 0.62f);
            actor.GetComponent<Renderer>().sharedMaterial = opponent ? red : blue;
            RemoveCollider(actor);
            return actor.transform;
        }

        public Transform Ball(Vector3 position)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Balón";
            ball.transform.SetParent(Actors, false);
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * 0.46f;
            ball.GetComponent<Renderer>().sharedMaterial = white;
            // The ball keeps its collider for precise mouse selection.
            return ball.transform;
        }

        public void ReceptionRing(Vector3 center, float radius) => Circle("Zona de recepción · afinidad", center, radius, gold, Actors);
        public void FinishLine(float z) => Cube("Zona objetivo · regate", new Vector3(0, 0.065f, z), new Vector3(11, 0.04f, 0.16f), gold, Actors);

        public LineRenderer AimLine(string name, Color color)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            go.transform.SetParent(Actors, false);
            var lr = go.GetComponent<LineRenderer>();
            lr.sharedMaterial = Material(name, color);
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.065f;
            lr.useWorldSpace = true;
            lr.enabled = false;
            return lr;
        }

        public void ClearActors()
        {
            for (int i = Actors.childCount - 1; i >= 0; i--)
            {
                var actor = Actors.GetChild(i).gameObject;
                actor.SetActive(false);
                Destroy(actor);
            }
        }

        // The editor stores generated materials as assets before saving the scene.
        public void ReleaseSavedMaterialOwnership() => ownedMaterials.Clear();

        private void Mark(Vector3 p, Vector3 s, Material m) => Cube("Marca de cancha", p, s, m, transform);
        private GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(go);
            return go;
        }
        private void Circle(string name, Vector3 center, float radius, Material material, Transform parent)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            go.transform.SetParent(parent, false);
            var lr = go.GetComponent<LineRenderer>();
            lr.sharedMaterial = material;
            lr.positionCount = 64;
            lr.loop = true;
            lr.startWidth = lr.endWidth = 0.045f;
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2f / 64f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0.06f, Mathf.Sin(a) * radius));
            }
        }
        private Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            material.SetColor("_BaseColor", color);
            ownedMaterials.Add(material);
            return material;
        }
        private static void RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
        private void OnDestroy()
        {
            foreach (var material in ownedMaterials)
                if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
