using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UltimaJugada.Tests
{
    public sealed class ProjectGenerationTests
    {
        [TestCase("Cesped claro", "Césped claro")]
        [TestCase("Cesped oscuro", "Césped oscuro")]
        public void GenerationRepairsInternalMaterialNameWhenFilenameIsAlreadyNormalized(string filename, string legacyName)
        {
            string path = "Assets/Materials/UltimaJugada/" + filename + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.That(material, Is.Not.Null);
            string guid = AssetDatabase.AssetPathToGUID(path);
            string scene = File.ReadAllText("Assets/Scenes/Match.unity");
            try
            {
                material.name = legacyName;
                EditorUtility.SetDirty(material);
                // Deliberately reproduce the invalid saved name before verifying its repair.
                LogAssert.Expect(LogType.Warning, $"Main Object Name '{legacyName}' does not match filename '{filename}'");
                AssetDatabase.SaveAssetIfDirty(material);

                Editor.ProjectBootstrap.Generate();

                Assert.That(material.name, Is.EqualTo(filename));
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
                Assert.That(File.ReadAllText("Assets/Scenes/Match.unity"), Is.EqualTo(scene));
                StringAssert.Contains("m_Name: " + filename, File.ReadAllText(path));
            }
            finally
            {
                material.name = filename;
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
        }

        [Test]
        public void RepeatedGenerationPreservesScenesConfigurationAndGuids()
        {
            string[] paths = new[]
            {
                "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Match.unity", "Assets/Scenes/MatchResult.unity",
                "Assets/Scenes/MainMenu.unity.meta", "Assets/Scenes/Match.unity.meta", "Assets/Scenes/MatchResult.unity.meta",
                "Assets/Resources/MatchConfig.asset", "Assets/Resources/MatchConfig.asset.meta"
            };
            var before = paths.Select(File.ReadAllText).ToArray();
            Editor.ProjectBootstrap.Generate();
            Editor.ProjectBootstrap.Generate();
            CollectionAssert.AreEqual(before, paths.Select(File.ReadAllText).ToArray());
            CollectionAssert.AreEqual(new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Match.unity", "Assets/Scenes/MatchResult.unity" },
                EditorBuildSettings.scenes.Take(3).Select(s => s.path).ToArray());
            Assert.That(EditorBuildSettings.scenes.Take(3).All(s => s.enabled), Is.True);
        }
    }
}
