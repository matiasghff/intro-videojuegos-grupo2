using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltimaJugada
{
    /// <summary>Procedural stadium and readable, animated footballers; no external art dependencies.</summary>
    public sealed class FieldView : MonoBehaviour
    {
        public const float HalfWidth = 5.5f;
        public const float Bottom = -9f;
        public const float Top = 9f;
        public const float GoalLine = 7.7f;
        public const float GoalHalfWidth = 2.1f;
        public Camera FieldCamera { get; private set; }
        public Transform Actors { get; private set; }
        [SerializeField] private Shader litShader;
        private Material blue, red, white, gold, skin, dark, shadow, keeperKit, arrow, drag;
        private readonly List<Material> ownedMaterials = new List<Material>();

        public void Initialize()
        {
            FieldCamera = GetComponentInChildren<Camera>();
            // Existing saved scenes keep their GUIDs. Upgrade their old presentation at runtime.
            if (transform.Find("Estadio v2") == null)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    var child = transform.GetChild(i).gameObject;
                    if (child.GetComponent<Camera>() != null) continue;
                    child.SetActive(false);
                    Destroy(child);
                }
                BuildEnvironment();
            }
            ConfigureCamera(FieldCamera);
            Actors = new GameObject("Interactive actors").transform;
            Actors.SetParent(transform, false);
            blue = Material("Local azul", new Color(0.09f, 0.49f, 0.95f), true);
            red = Material("Rival coral", new Color(0.95f, 0.20f, 0.25f), true);
            white = Material("Balon blanco", new Color(0.97f, 0.98f, 0.94f), true);
            gold = Material("Objetivo dorado", UIFactory.Accent);
            skin = Material("Piel", new Color(0.80f, 0.54f, 0.36f), true);
            dark = Material("Botines y pantalon", new Color(0.035f, 0.06f, 0.11f), true);
            shadow = Material("Sombra de contacto", new Color(0.045f, 0.15f, 0.12f));
            keeperKit = Material("Arquero naranja", new Color(1f, 0.64f, 0.08f), true);
            arrow = Material("Flecha direccion", UIFactory.Accent);
            drag = Material("Indicador arrastre", new Color(1f, 0.79f, 0.25f));
        }

        public static FieldView Build(Transform parent)
        {
            var root = new GameObject("Cancha y estadio");
            root.transform.SetParent(parent, false);
            var field = root.AddComponent<FieldView>();
            field.litShader = Shader.Find("Universal Render Pipeline/Lit");
            var camera = new GameObject("Field Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(root.transform, false);
            camera.tag = "MainCamera";
            ConfigureCamera(camera);
            field.BuildEnvironment();
            return field;
        }

        private static void ConfigureCamera(Camera camera)
        {
            camera.transform.SetPositionAndRotation(new Vector3(0f, 24f, -10f), Quaternion.LookRotation(new Vector3(0f, -24f, 10.5f)));
            camera.orthographic = true;
            camera.orthographicSize = 10.4f;
            camera.rect = new Rect(0.015f, 0.15f, 0.70f, 0.69f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIFactory.Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
        }

        private void BuildEnvironment()
        {
            var stadium = new GameObject("Estadio v2").transform;
            stadium.SetParent(transform, false);
            var grassA = Material("Cesped claro", new Color(0.16f, 0.43f, 0.28f), true);
            var grassB = Material("Cesped oscuro", new Color(0.12f, 0.36f, 0.23f), true);
            var line = Material("Marcas", new Color(0.87f, 0.96f, 0.86f));
            var net = Material("Red", new Color(0.53f, 0.68f, 0.70f));
            var concrete = Material("Hormigon estadio", new Color(0.065f, 0.10f, 0.15f), true);
            var seats = Material("Asientos azules", new Color(0.12f, 0.25f, 0.37f), true);
            var seatsLight = Material("Asientos claros", new Color(0.26f, 0.42f, 0.50f), true);
            var boards = Material("Publicidad", new Color(0.08f, 0.19f, 0.24f));
            var accent = Material("Acento estadio", UIFactory.Accent);
            Cube("Base del estadio", new Vector3(0f, -0.45f, 0f), new Vector3(42f, 0.5f, 30f), concrete, stadium);
            Cube("Perimetro verde", new Vector3(0f, -0.20f, 0f), new Vector3(13f, 0.25f, 20f), grassB, stadium);
            for (int i = 0; i < 18; i++)
                Cube("Corte de cesped", new Vector3(0f, -0.05f, -8.5f + i), new Vector3(11.7f, 0.1f, 1f), i % 2 == 0 ? grassA : grassB, stadium);
            Mark(new Vector3(-HalfWidth, 0.025f, 0f), new Vector3(0.055f, 0.025f, 18f), line, stadium);
            Mark(new Vector3(HalfWidth, 0.025f, 0f), new Vector3(0.055f, 0.025f, 18f), line, stadium);
            foreach (float z in new[] { Bottom, 0f, Top })
                Mark(new Vector3(0f, 0.025f, z), new Vector3(11f, 0.025f, 0.055f), line, stadium);
            Circle("Circulo central", Vector3.zero, 1.6f, line, stadium);
            Cube("Punto central", new Vector3(0f, 0.04f, 0f), new Vector3(0.15f, 0.02f, 0.15f), line, stadium);
            foreach (int sign in new[] { -1, 1 })
            {
                Mark(new Vector3(0f, 0.025f, sign * 5.5f), new Vector3(7f, 0.025f, 0.055f), line, stadium);
                foreach (float x in new[] { -3.5f, 3.5f })
                    Mark(new Vector3(x, 0.025f, sign * 7.25f), new Vector3(0.055f, 0.025f, 3.5f), line, stadium);
                foreach (float x in new[] { -2f, 2f })
                    Mark(new Vector3(x, 0.025f, sign * 8.25f), new Vector3(0.055f, 0.025f, 1.5f), line, stadium);
                Mark(new Vector3(0f, 0.025f, sign * 7.5f), new Vector3(4f, 0.025f, 0.055f), line, stadium);
                for (int row = 0; row < 3; row++)
                {
                    Cube("Grada", new Vector3(sign * (7.2f + row * 0.8f), row * 0.35f, 0f), new Vector3(0.8f, 0.4f + row * 0.65f, 20f), concrete, stadium);
                    for (int seat = 0; seat < 26; seat++)
                        Cube("Butacas", new Vector3(sign * (7.2f + row * 0.8f), 0.3f + row * 0.7f, -9.25f + seat * 0.74f), new Vector3(0.48f, 0.17f, 0.48f), (seat + row) % 4 == 0 ? seatsLight : seats, stadium);
                }
                for (int i = 0; i < 6; i++)
                {
                    Cube("Valla", new Vector3(sign * 6.2f, 0.22f, -7.5f + i * 3f), new Vector3(0.15f, 0.44f, 2.75f), boards, stadium);
                    Cube("Franja valla", new Vector3(sign * 6.1f, 0.46f, -7.5f + i * 3f), new Vector3(0.17f, 0.06f, 2.75f), i % 2 == 0 ? accent : line, stadium);
                }
                foreach (float x in new[] { -5.5f, 5.5f })
                {
                    Cube("Banderin poste", new Vector3(x, 0.4f, sign * 9f), new Vector3(0.035f, 0.8f, 0.035f), line, stadium);
                    Cube("Banderin", new Vector3(x + 0.12f, 0.70f, sign * 9f), new Vector3(0.25f, 0.18f, 0.03f), accent, stadium);
                }
            }
            Goal(stadium, line, net, 1);
            Goal(stadium, line, net, -1);
            var light = new GameObject("Luz de estadio", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(stadium, false);
            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
        }

        private void Goal(Transform parent, Material line, Material net, int sign)
        {
            Cube("Linea de gol", new Vector3(0f, 0.04f, sign * GoalLine), new Vector3(4.2f, 0.04f, 0.08f), line, parent);
            foreach (float x in new[] { -GoalHalfWidth, GoalHalfWidth })
            {
                Cube("Poste", new Vector3(x, 0.65f, sign * GoalLine), new Vector3(0.12f, 1.3f, 0.12f), line, parent);
                Cube("Soporte trasero", new Vector3(x, 0.65f, sign * 8.8f), new Vector3(0.06f, 1.3f, 0.06f), net, parent);
            }
            Cube("Travesano", new Vector3(0f, 1.3f, sign * GoalLine), new Vector3(4.3f, 0.12f, 0.12f), line, parent);
            for (int i = 0; i <= 14; i++)
            {
                Cube("Red suelo", new Vector3(-2.1f + i * 0.3f, 0.10f, sign * 8.25f), new Vector3(0.018f, 0.02f, 1.1f), net, parent);
                Cube("Red fondo vertical", new Vector3(-2.1f + i * 0.3f, 0.65f, sign * 8.8f), new Vector3(0.018f, 1.3f, 0.018f), net, parent);
            }
            for (int i = 0; i <= 6; i++)
                Cube("Red fondo horizontal", new Vector3(0f, i * 0.21f, sign * 8.8f), new Vector3(4.2f, 0.018f, 0.018f), net, parent);
        }

        public Transform Player(string name, Vector3 position, bool opponent = false)
        {
            var root = new GameObject(name).transform;
            root.SetParent(Actors, false);
            root.position = position;
            if (opponent) root.rotation = Quaternion.Euler(0f, 180f, 0f);
            bool isKeeper = name.Contains("Portero");
            Material jersey = isKeeper ? keeperKit : opponent ? red : blue;
            Part("Sombra", PrimitiveType.Cylinder, root, new Vector3(0.10f, -0.565f, 0f), new Vector3(0.78f, 0.008f, 0.58f), shadow);
            Part("Camiseta", PrimitiveType.Capsule, root, new Vector3(0f, 0.10f, 0f), new Vector3(0.51f, 0.32f, 0.35f), jersey);
            Part("Franja camiseta", PrimitiveType.Cube, root, new Vector3(0f, 0.12f, -0.17f), new Vector3(0.10f, 0.36f, 0.025f), white);
            Part("Cabeza", PrimitiveType.Sphere, root, new Vector3(0f, 0.53f, 0f), new Vector3(0.31f, 0.34f, 0.31f), skin);
            Part("Pelo", PrimitiveType.Sphere, root, new Vector3(0f, 0.63f, -0.015f), new Vector3(0.32f, 0.18f, 0.32f), dark);
            Part("Pantalon", PrimitiveType.Cube, root, new Vector3(0f, -0.15f, 0f), new Vector3(0.40f, 0.22f, 0.30f), dark);
            var left = Limb(root, -1f, jersey);
            var right = Limb(root, 1f, jersey);
            root.gameObject.AddComponent<FootballerVisual>().Initialize(left, right);
            if (name.StartsWith("Tú"))
            {
                var ring = Circle("Jugador controlado", new Vector3(position.x, 0.065f, position.z), 0.48f, gold, root);
                ring.useWorldSpace = false;
                for (int i = 0; i < ring.positionCount; i++) ring.SetPosition(i, ring.GetPosition(i) - position);
            }
            return root;
        }

        private Transform Limb(Transform root, float side, Material jersey)
        {
            var leg = Part("Pierna", PrimitiveType.Capsule, root, new Vector3(side * 0.13f, -0.34f, 0f), new Vector3(0.16f, 0.18f, 0.17f), white);
            Part("Botin", PrimitiveType.Cube, leg, new Vector3(0f, -0.8f, 0.25f), new Vector3(1.15f, 0.50f, 1.7f), dark);
            Part("Brazo", PrimitiveType.Capsule, root, new Vector3(side * 0.34f, 0.06f, 0f), new Vector3(0.15f, 0.24f, 0.17f), jersey);
            Part("Mano", PrimitiveType.Sphere, root, new Vector3(side * 0.34f, -0.16f, 0.01f), Vector3.one * 0.15f, skin);
            return leg;
        }

        public Transform Ball(Vector3 position)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Balón";
            ball.transform.SetParent(Actors, false);
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * 0.46f;
            ball.GetComponent<Renderer>().sharedMaterial = white;
            foreach (Vector3 normal in new[] { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                Part("Panel negro", PrimitiveType.Sphere, ball.transform, normal * 0.45f, Vector3.one * 0.30f, dark);
            // Only the outer sphere keeps its collider, for reliable mouse selection.
            return ball.transform;
        }

        public void ReceptionRing(Vector3 center, float radius) => Circle("Zona de recepcion", center, radius, gold, Actors);
        public void FinishLine(float z) => Cube("Zona objetivo", new Vector3(0f, 0.065f, z), new Vector3(11f, 0.04f, 0.14f), gold, Actors);

        public LineRenderer AimLine(string name, Color color)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            go.transform.SetParent(Actors, false);
            var lr = go.GetComponent<LineRenderer>();
            // Share line materials across interventions instead of allocating every time.
            lr.sharedMaterial = name.StartsWith("Arrastre") ? drag : arrow;
            lr.startColor = lr.endColor = color;
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.08f;
            lr.numCapVertices = 4;
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
        public void ReleaseSavedMaterialOwnership() => ownedMaterials.Clear();
        private void Mark(Vector3 p, Vector3 s, Material m, Transform parent) => Cube("Marca de cancha", p, s, m, parent);
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
        private Transform Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(go);
            return go.transform;
        }
        private LineRenderer Circle(string name, Vector3 center, float radius, Material material, Transform parent)
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
            return lr;
        }
        private Material Material(string name, Color color, bool lit = false)
        {
            var shader = lit ? (litShader != null ? litShader : Shader.Find("Universal Render Pipeline/Lit")) : Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            if (lit) material.SetFloat("_Smoothness", 0.15f);
            ownedMaterials.Add(material);
            return material;
        }
        private static void RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c == null) return;
            c.enabled = false;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
        private void OnDestroy()
        {
            foreach (var material in ownedMaterials)
                if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
