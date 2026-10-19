using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaJugada.Editor
{
    public static class ProjectBootstrap
    {
        private const string SceneFolder = "Assets/Scenes/";
        private static readonly string[] Names = { "MainMenu", "Match", "MatchResult" };

        [InitializeOnLoadMethod]
        private static void ScheduleSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Application.isBatchMode) return;
                if (Names.Any(name => !File.Exists(SceneFolder + name + ".unity"))) Generate();
                else RepairMaterialNames();
                SetStartScene();
            };
        }

        [MenuItem("Tools/Ultima Jugada/Generate or Repair Project")]
        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Materials/UltimaJugada");
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            RepairMaterialNames();
            if (AssetDatabase.LoadAssetAtPath<MatchConfig>("Assets/Resources/MatchConfig.asset") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MatchConfig>(), "Assets/Resources/MatchConfig.asset");

            foreach (string name in Names)
                if (!File.Exists(SceneFolder + name + ".unity")) CreateScene(name);

            var ownPaths = Names.Select(name => SceneFolder + name + ".unity").ToArray();
            var otherScenes = EditorBuildSettings.scenes.Where(s => !ownPaths.Contains(s.path));
            EditorBuildSettings.scenes = ownPaths.Select(path => new EditorBuildSettingsScene(path, true)).Concat(otherScenes).ToArray();
            PlayerSettings.companyName = "Grupo 2 - UANDES";
            // ASCII executable name avoids NFC/NFD resource-seal mismatches in macOS codesign.
            // The UI uses the same unaccented title for consistency.
            PlayerSettings.productName = "Ultima Jugada";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "cl.uandes.grupo2.ultimajugada");
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            SetStartScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Ultima Jugada: tres escenas listas. Presiona Play. Se conservaron las escenas y configuraciones existentes.");
        }

        private static void CreateScene(string name)
        {
            var previous = SceneManager.GetActiveScene();
            // Batch starts with an empty untitled scene. Unity cannot add a scene beside it.
            bool emptyUntitled = string.IsNullOrEmpty(previous.path) && previous.rootCount == 0;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, emptyUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject(name + " · Controller");
            if (name == "Match")
            {
                var manager = root.AddComponent<MatchSimulationManager>();
                var field = FieldView.Build(root.transform);
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("field").objectReferenceValue = field;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PersistMaterials(root);
                field.ReleaseSavedMaterialOwnership();
            }
            else
            {
                if (name == "MainMenu") root.AddComponent<MainMenuUI>(); else root.AddComponent<MatchResultUI>();
                var camera = new GameObject("UI Camera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.SetParent(root.transform, false);
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = UIFactory.Background;
                camera.orthographic = true;
            }
            EditorSceneManager.SaveScene(scene, SceneFolder + name + ".unity");
            if (!emptyUntitled)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [MenuItem("Tools/Ultima Jugada/Refresh Match Presentation")]
        public static void RefreshMatchPresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string path = SceneFolder + "Match.unity";
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(path);
            bool alreadyOpen = scene.IsValid() && scene.isLoaded;
            if (!alreadyOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var manager = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MatchSimulationManager>()).First();
            var oldField = manager.GetComponentInChildren<FieldView>();
            if (oldField != null) Object.DestroyImmediate(oldField.gameObject);
            var field = FieldView.Build(manager.transform);
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("field").objectReferenceValue = field;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PersistMaterials(field.gameObject, true);
            field.ReleaseSavedMaterialOwnership();
            EditorSceneManager.SaveScene(scene, path);
            if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        private static void PersistMaterials(GameObject root, bool updateExisting = false)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var material = renderer.sharedMaterial;
                if (material == null || AssetDatabase.Contains(material)) continue;
                string path = "Assets/Materials/UltimaJugada/" + material.name + ".mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null) AssetDatabase.CreateAsset(material, path);
                else
                {
                    if (updateExisting)
                    {
                        existing.shader = material.shader;
                        existing.CopyPropertiesFromMaterial(material);
                        EditorUtility.SetDirty(existing);
                    }
                    renderer.sharedMaterial = existing;
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void RepairMaterialNames()
        {
            NormalizeMaterialName("Césped claro", "Cesped claro");
            NormalizeMaterialName("Césped oscuro", "Cesped oscuro");
        }

        private static void NormalizeMaterialName(string oldName, string newName)
        {
            const string folder = "Assets/Materials/UltimaJugada/";
            string oldPath = folder + oldName + ".mat", newPath = folder + newName + ".mat";
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (!string.IsNullOrEmpty(error)) throw new BuildFailedException(error);
            }
            // Renaming the file alone leaves the serialized Material.name unchanged.
            var material = AssetDatabase.LoadAssetAtPath<Material>(newPath);
            if (material == null || material.name == newName) return;
            material.name = newName;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        private static void SetStartScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneFolder + "MainMenu.unity");
            if (scene != null) EditorSceneManager.playModeStartScene = scene;
        }

        [MenuItem("Tools/Ultima Jugada/Build macOS")]
        public static void BuildMacOS()
        {
            Generate();
#if UNITY_EDITOR_OSX
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64ARM64;
#endif
            var options = new BuildPlayerOptions
            {
                scenes = Names.Select(name => SceneFolder + name + ".unity").ToArray(),
                locationPathName = "Builds/macOS/UltimaJugada.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("No se pudo compilar macOS: " + report.summary.result);
#if UNITY_EDITOR_OSX
            var verify = new System.Diagnostics.ProcessStartInfo("/usr/bin/codesign",
                "--verify --deep --strict \"" + Path.GetFullPath(options.locationPathName) + "\"")
            { UseShellExecute = false, RedirectStandardError = true };
            using (var process = System.Diagnostics.Process.Start(verify))
            {
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new BuildFailedException("Firma local macOS inválida: " + error);
            }
#endif
            Debug.Log($"Build macOS correcto: {report.summary.totalSize} bytes.");
        }

        [MenuItem("Tools/Ultima Jugada/Export Entrega 4")]
        public static void ExportDelivery()
        {
            Generate();
            Directory.CreateDirectory("Builds/Entrega4/ProjectSettings");
            Directory.CreateDirectory("Builds/Entrega4/Packages");
            AssetDatabase.ExportPackage("Assets", "Builds/Entrega4/UltimaJugada.unitypackage", ExportPackageOptions.Recurse);
            foreach (string file in Directory.GetFiles("ProjectSettings"))
                File.Copy(file, "Builds/Entrega4/ProjectSettings/" + Path.GetFileName(file), true);
            foreach (string file in Directory.GetFiles("Packages", "*.json"))
                File.Copy(file, "Builds/Entrega4/Packages/" + Path.GetFileName(file), true);
            CopyDeliveryDocuments();
            Debug.Log("Entrega exportada en Builds/Entrega4. Comprime esa carpeta como ZIP y súbela al Drive del curso.");
        }

        private static void CopyDeliveryDocuments()
        {
            string repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string source = Path.Combine(repository, "docs", "Entrega4_Informe.pdf");
            const string destination = "Builds/Entrega4/docs/Entrega4_Informe.pdf";
            bool hasReport = File.Exists(source);
            if (hasReport)
            {
                Directory.CreateDirectory("Builds/Entrega4/docs");
                File.Copy(source, destination, true);
            }
            else if (File.Exists(destination)) File.Delete(destination);

            string readme = "# Ultima Jugada - Entrega 4\n\n"
                + "Contenido: UltimaJugada.unitypackage, ProjectSettings/ y Packages/.\n\n"
                + (hasReport ? "El informe actualizado esta en docs/Entrega4_Informe.pdf y debe entregarse tambien por separado.\n\n" : "El informe PDF se entrega por separado.\n\n")
                + "## Recuperar el proyecto en Unity\n\n"
                + "1. Crear un proyecto URP con Unity 6000.5.10f1 y cerrarlo.\n"
                + "2. Copiar ProjectSettings/ y Packages/ a la raiz de ese proyecto nuevo.\n"
                + "3. Abrirlo y esperar la resolucion de paquetes.\n"
                + "4. Usar Assets > Import Package > Custom Package e importar UltimaJugada.unitypackage completo.\n"
                + "5. Ejecutar Tools > Ultima Jugada > Generate or Repair Project.\n"
                + "6. Abrir Assets/Scenes/MainMenu.unity y presionar Play.\n\n"
                + "Regate: WASD/flechas. Pase/tiro: clic en el balon, arrastrar hacia atras, soltar y clicar el centro del balon de precision.\n\n"
                + "La guia completa y la documentacion tecnica estan en docs/ del repositorio.\n";
            File.WriteAllText("Builds/Entrega4/LEEME_ENTREGA.md", readme);
        }
    }
}
